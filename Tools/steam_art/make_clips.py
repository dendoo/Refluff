"""Steam description clips from StoreGifApi takes (Docs/STORE_ART.md).

    python Tools/steam_art/make_clips.py OUT.mp4 TAKE [TAKE ...] [options]

TAKE is a folder written by StoreGifApi (f0001.png ... plus f0001_ui.png on HUD takes, and seq.json) or just the take
name, looked up in SteamStore/_work/clips/. Several takes play back to back. OUT without a folder goes to
SteamStore/description/. Writes OUT (MP4: H.264 High, yuv420p, BT.709 tagged, no audio track, +faststart),
OUT-stem_poster.png (the first frame) and OUT-stem_sheet.png (12 evenly spaced frames), and prints one JSON line
with the duration, fps, frame count, bytes and size, read back from the file with ffmpeg.

Per frame, in this order:
  1. composite  HUD takes: the scene crop is downscaled (Lanczos, linear light) to the panel crop's size and the
                premultiplied panel is laid over it in 8-bit sRGB (over = ui + (1 - ui.a) * scene), exactly as
                compose_screens.py builds the store screenshots and EconomyHud.DebugCapture composites in game.
  2. --crop     optional extra crop x,y,w,h in the take's frame pixels (the scene crop saved by StoreGifApi).
  3. fit        centre-crop to the output aspect (only when --height forces one, or when a later take's aspect differs).
  4. downscale  Lanczos in LINEAR light to --width (1560 = 2x Steam's 780 px column), so thin bright fibres and the
                grout do not darken.
Timeline: every frame is shown from its Step (seq.json frameSteps x stepDt) until the next one; the result is sampled
at a constant --fps (25 = one frame per 2 Steps), so a cadence change (StoreGifApi.Every) keeps real time by
repeating frames. --hold-last adds a still at the end; --fade-loop crossfades the end into the start for a seamless
loop (the clip gets shorter by that much: it starts --fade-loop seconds in). Clips of 11.5 s or more are refused
(Steam: "under 12 seconds") unless --force. --gif OUT.gif also writes the GIF fallback (gif_encoder.py); a bare
name goes to SteamStore/_work/clips/, not description/, and --gif-budget-mb counts 1e6 bytes like verify_assets.py.

Options: --width 1560  --height 0 (= from the aspect)  --crop x,y,w,h  --fps 25  --hold-last 0  --fade-loop 0
         --crf 18  --preset slow  --max-seconds 11.5  --force  --jobs N  --gif OUT.gif  --gif-width 780
         --gif-budget-mb 5  --gif-dither blue|bayer|none|fs
"""
import argparse
import bisect
import json
import os
import re
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from fractions import Fraction
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from clip_probe import ffmpeg_exe, probe_media  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
CLIPS = ROOT / "SteamStore" / "_work" / "clips"
DESCRIPTION = ROOT / "SteamStore" / "description"
FRAME_RE = re.compile(r"^f(\d+)\.png$", re.IGNORECASE)   # scene frames; fNNNN_ui.png panels do not match


def _ranges(nums):
    """[1, 2, 3, 7, 9, 10] -> '1-3, 7, 9-10' (for warnings)."""
    out, i = [], 0
    while i < len(nums):
        j = i
        while j + 1 < len(nums) and nums[j + 1] == nums[j] + 1:
            j += 1
        out.append(str(nums[i]) if i == j else "%d-%d" % (nums[i], nums[j]))
        i = j + 1
    return ", ".join(out)

# ------------------------------------------------------------------------------------------------ colour
_S2L = None


def srgb_u8_to_linear(a):
    """uint8 sRGB -> float32 linear through a 256-entry table (exact, and fast on 4K frames)."""
    global _S2L
    if _S2L is None:
        c = np.arange(256, dtype=np.float64) / 255.0
        _S2L = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4).astype(np.float32)
    return _S2L[a]


def linear_to_srgb_u8(f):
    f = np.clip(f, 0.0, 1.0)
    s = np.where(f <= 0.0031308, f * 12.92, 1.055 * np.power(f, 1.0 / 2.4) - 0.055)
    return np.clip(np.rint(s * 255.0), 0, 255).astype(np.uint8)


def resize_linear(rgb_u8, w, h):
    """Lanczos resize of an sRGB uint8 HxWx3 image in linear light (PIL 'F' per channel), back to sRGB uint8."""
    if rgb_u8.shape[1] == w and rgb_u8.shape[0] == h:
        return rgb_u8
    lin = srgb_u8_to_linear(rgb_u8)
    out = np.empty((h, w, 3), np.float32)
    for c in range(3):
        out[..., c] = np.asarray(Image.fromarray(np.ascontiguousarray(lin[..., c]), "F").resize((w, h), Image.Resampling.LANCZOS))
    return linear_to_srgb_u8(out)


def composite_ui(scene_u8, ui_rgba_u8):
    """Premultiplied UI Toolkit panel over the scene, 8-bit sRGB: over = ui + (1 - ui.a) * scene (compose_screens.py)."""
    a = ui_rgba_u8[..., 3:4].astype(np.float32) / 255.0
    out = ui_rgba_u8[..., :3].astype(np.float32) / 255.0 + (1.0 - a) * (scene_u8.astype(np.float32) / 255.0)
    return (np.clip(out, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8)


def fit_aspect(img, w, h):
    """Centre-crop img to the w:h aspect (no-op when it already matches to within a pixel)."""
    ih, iw = img.shape[:2]
    if abs(iw * h - ih * w) <= max(w, h):
        return img
    if iw * h > ih * w:
        nw = max(1, int(round(ih * w / float(h))))
        x0 = (iw - nw) // 2
        return img[:, x0:x0 + nw]
    nh = max(1, int(round(iw * h / float(w))))
    y0 = (ih - nh) // 2
    return img[y0:y0 + nh]


# ------------------------------------------------------------------------------------------------ takes
class Take:
    """One StoreGifApi folder: frame paths, their times (s, from the first frame) and the layer geometry."""

    def __init__(self, arg):
        p = Path(arg)
        if not p.is_dir():
            p = CLIPS / arg
        if not p.is_dir():
            raise SystemExit("take not found: %s (looked in %s too)" % (arg, CLIPS))
        self.dir = p
        self.name = p.name
        # fNNNN.png, NNNN = 1-based capture number (the index into seq.json frameSteps)
        numbered = []
        for q in p.glob("f*.png"):
            m = FRAME_RE.match(q.name)
            if m:
                numbered.append((int(m.group(1)), q))
        numbered.sort()
        if not numbered:
            raise SystemExit("no frames in " + str(p))
        meta = p / "seq.json"
        self.seq = json.loads(meta.read_text(encoding="utf-8")) if meta.exists() else {}
        if not self.seq:
            print("warning: %s has no seq.json (take not ended with SeqEnd?): 25 fps assumed" % p, file=sys.stderr)
        step_dt = float(self.seq.get("stepDt", 0.02))
        frame_steps = list(self.seq.get("frameSteps", []))
        cad = self.seq.get("cadence") or [{"fromFrame": 1, "every": 2}]
        last_every = int(cad[-1].get("every", 2))
        # every file keeps its own Step: a missing file only makes the frame before it stay up longer, it never retimes
        # the rest (a cadence change or an Every(3) part keeps its speed)
        self.frames, steps = [], []
        if frame_steps:
            have = {n for n, _ in numbered}
            missing = [n for n in range(1, len(frame_steps) + 1) if n not in have]
            extra = [n for n, _ in numbered if n > len(frame_steps)]
            if missing:
                print("warning: %s: %d of %d frames missing (%s), the frame before each stays up longer"
                      % (p, len(missing), len(frame_steps), _ranges(missing)), file=sys.stderr)
            if extra:
                print("warning: %s: frames %s are not in seq.json frameSteps, left out" % (p, _ranges(extra)), file=sys.stderr)
            for n, q in numbered:
                if n <= len(frame_steps):
                    self.frames.append(q)
                    steps.append(int(frame_steps[n - 1]))
            if not self.frames:
                raise SystemExit("no frames of %s match its seq.json" % p)
        else:
            for n, q in numbered:
                self.frames.append(q)
                steps.append(2 * n)
        self.times = [(s - steps[0]) * step_dt for s in steps]
        self.last_dur = last_every * step_dt
        self.duration = self.times[-1] + self.last_dur
        self.hud = bool(self.seq.get("hud", False))
        self.errors = self.seq.get("errors", [])

    def ui_path(self, i):
        q = self.frames[i]
        u = q.with_name(q.stem + "_ui.png")
        return u if u.exists() else None


# ------------------------------------------------------------------------------------------------ frames
class Processor:
    """Take frame -> finished W x H sRGB uint8 output frame (composite, crop, fit, linear-light downscale)."""

    def __init__(self, W, H, crop):
        self.W, self.H, self.crop = W, H, crop
        self.warned = set()

    def warn(self, key, msg):
        if key not in self.warned:
            self.warned.add(key)
            print("warning: " + msg, file=sys.stderr)

    def __call__(self, take, i):
        scene = np.asarray(Image.open(take.frames[i]).convert("RGB"))
        scale = 1.0
        up = take.ui_path(i)
        if up is not None:
            ui = np.asarray(Image.open(up).convert("RGBA"))
            uh, uw = ui.shape[:2]
            if abs(scene.shape[1] * uh - scene.shape[0] * uw) > max(uw, uh):
                self.warn(("aspect", take.name), "%s: scene %dx%d and panel %dx%d crops differ in aspect"
                          % (take.name, scene.shape[1], scene.shape[0], uw, uh))
            img = composite_ui(resize_linear(scene, uw, uh), ui)   # the scene at the panel's pixel size, then over
            scale = uw / float(scene.shape[1])
        else:
            if take.hud:
                self.warn(("noui", take.name), "%s is a HUD take but some frames have no _ui.png (scene only there)" % take.name)
            img = scene
        if self.crop:
            x, y, w, h = [int(round(v * scale)) for v in self.crop]
            ih, iw = img.shape[:2]
            x, y = max(0, min(x, iw - 1)), max(0, min(y, ih - 1))
            img = img[y:y + h, x:x + w]
        img = fit_aspect(img, self.W, self.H)
        if img.shape[1] < self.W:
            self.warn(("up", take.name), "%s: %d px of source for a %d px clip (upscaled)" % (take.name, img.shape[1], self.W))
        return resize_linear(np.ascontiguousarray(img), self.W, self.H)


def blend_linear(a, b, w):
    """(1 - w) * a + w * b in linear light (the loop crossfade)."""
    return linear_to_srgb_u8(srgb_u8_to_linear(a) * (1.0 - w) + srgb_u8_to_linear(b) * w)


def build_timeline(takes, fps, hold):
    """Segments (take, frame, start s) back to back; returns (segments, starts, total seconds)."""
    segs, starts, t0 = [], [], 0.0
    for ti, tk in enumerate(takes):
        for fi, t in enumerate(tk.times):
            segs.append((ti, fi))
            starts.append(t0 + t)
        t0 += tk.duration
    return segs, starts, t0 + max(0.0, hold)


def sample(starts, n, fps):
    """Constant-fps sampling: output frame k shows the segment that covers the middle of its interval."""
    out = []
    for k in range(n):
        t = (k + 0.5) / fps
        j = bisect.bisect_right(starts, t + 1e-9) - 1
        out.append(min(max(j, 0), len(starts) - 1))
    return out


def contact_sheet(thumbs, W, H, path, cols=4):
    """thumbs = [(output frame index, seconds, uint8 frame)] -> one PNG grid with the time under every frame."""
    tw = W // cols
    th = int(round(H * tw / float(W)))
    rows = (len(thumbs) + cols - 1) // cols
    gap, lab = 6, 18
    sheet = Image.new("RGB", (cols * tw + (cols + 1) * gap, rows * (th + lab) + (rows + 1) * gap), (24, 24, 28))
    d = ImageDraw.Draw(sheet)
    for n, (k, sec, fr) in enumerate(thumbs):
        x = gap + (n % cols) * (tw + gap)
        y = gap + (n // cols) * (th + lab + gap)
        sheet.paste(Image.fromarray(resize_linear(fr, tw, th)), (x, y))
        d.text((x + 2, y + th + 3), "#%d  %.2f s" % (k, sec), fill=(220, 220, 220))
    sheet.save(path, optimize=True)


# ------------------------------------------------------------------------------------------------ main
def resolve_out(p, folder=DESCRIPTION):
    q = Path(p)
    if not q.is_absolute() and str(q.parent) in ("", "."):
        q = folder / q
    q.parent.mkdir(parents=True, exist_ok=True)
    return q


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("out")
    ap.add_argument("takes", nargs="+")
    ap.add_argument("--width", type=int, default=1560)
    ap.add_argument("--height", type=int, default=0)
    ap.add_argument("--crop", default="", help="x,y,w,h in the take's frame pixels")
    ap.add_argument("--fps", type=float, default=25.0)
    ap.add_argument("--hold-last", type=float, default=0.0)
    ap.add_argument("--fade-loop", type=float, default=0.0)
    ap.add_argument("--crf", type=int, default=18)
    ap.add_argument("--preset", default="slow")
    ap.add_argument("--max-seconds", type=float, default=11.5)
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--jobs", type=int, default=min(8, os.cpu_count() or 4))
    ap.add_argument("--gif", default="", help="also write this GIF (fallback)")
    ap.add_argument("--gif-width", type=int, default=780)
    ap.add_argument("--gif-budget-mb", type=float, default=5.0)
    ap.add_argument("--gif-dither", default="blue", choices=["blue", "bayer", "none", "fs"])
    a = ap.parse_args(argv)
    t_start = time.time()

    ff = ffmpeg_exe()
    if not ff:
        raise SystemExit("ffmpeg not available: pip install imageio-ffmpeg")
    takes = [Take(t) for t in a.takes]
    crop = tuple(int(v) for v in a.crop.split(",")) if a.crop else None
    if crop and (len(crop) != 4 or crop[2] < 2 or crop[3] < 2):
        raise SystemExit("--crop needs x,y,w,h")

    W = max(2, a.width - a.width % 2)
    if a.height > 0:
        H = max(2, a.height - a.height % 2)
    else:
        with Image.open(takes[0].frames[0]) as im0:
            sw, sh = im0.size
        cw, ch = (crop[2], crop[3]) if crop else (sw, sh)
        H = max(2, int(round(W * ch / float(cw) / 2.0)) * 2)

    segs, starts, total = build_timeline(takes, a.fps, a.hold_last)
    n = max(1, int(round(total * a.fps)))
    F = int(round(max(0.0, a.fade_loop) * a.fps))
    if F > 0 and 2 * F >= n:
        raise SystemExit("--fade-loop %.2f s is too long for a %.2f s clip" % (a.fade_loop, n / a.fps))
    src = sample(starts, n, a.fps)
    # plan = (segment shown, segment blended in, weight); with a loop fade the clip starts F frames in and its last F
    # frames dissolve into the first F, so the last frame leads straight into the first one
    plan = [(src[F + j], None, 0.0) for j in range(n - 2 * F)] + \
           [(src[n - F + j], src[j], (j + 1) / float(F + 1)) for j in range(F)]
    n_out = len(plan)
    seconds = n_out / a.fps
    if seconds >= a.max_seconds and not a.force:
        print(json.dumps({"error": "clip would be %.2f s, the limit is < %.2f s (Steam: under 12 s); shorten the take, "
                                   "lower --hold-last or pass --force" % (seconds, a.max_seconds)}))
        return 2

    out = resolve_out(a.out)
    stem = out.with_suffix("")
    poster_path = Path(str(stem) + "_poster.png")
    sheet_path = Path(str(stem) + "_sheet.png")
    fps_str = str(Fraction(a.fps).limit_denominator(1000))
    cmd = [ff, "-hide_banner", "-nostats", "-v", "error", "-y",
           "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H), "-framerate", fps_str, "-i", "-",
           "-an",
           # RGB -> Y'CbCr with the BT.709 matrix (swscale's default is BT.601), limited range; ffmpeg 7 takes the colour
           # tags from the frames, so setparams stamps all three there (the -color_* flags below alone leave primaries
           # and transfer "unknown")
           "-vf", "scale=out_color_matrix=bt709:out_range=tv:flags=lanczos+accurate_rnd+full_chroma_int,format=yuv420p,"
                  "setparams=range=tv:color_primaries=bt709:color_trc=bt709:colorspace=bt709",
           "-c:v", "libx264", "-preset", a.preset, "-crf", str(a.crf), "-profile:v", "high", "-pix_fmt", "yuv420p",
           "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709", "-color_range", "tv",
           "-movflags", "+faststart", str(out)]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stderr=subprocess.PIPE)

    process = Processor(W, H, crop)
    last_use = {}
    for p, (k0, k1, _) in enumerate(plan):
        last_use[k0] = p
        if k1 is not None:
            last_use[k1] = p
    sheet_at = set(np.linspace(0, n_out - 1, min(12, n_out)).round().astype(int).tolist())
    thumbs, gif_frames = [], []
    gw = max(2, a.gif_width)
    gh = max(2, int(round(gw * H / float(W))))
    futures, q, window = {}, 0, max(2, 2 * a.jobs)
    unique_frames = 0
    with ThreadPoolExecutor(max_workers=max(1, a.jobs)) as pool:
        for p, (k0, k1, w) in enumerate(plan):
            while q < n_out and (q <= p or len(futures) < window):
                for k in (plan[q][0], plan[q][1]):
                    if k is not None and k not in futures and last_use[k] >= q:
                        ti, fi = segs[k]
                        futures[k] = pool.submit(process, takes[ti], fi)
                        unique_frames += 1
                q += 1
            fr = futures[k0].result()
            if k1 is not None:
                fr = blend_linear(fr, futures[k1].result(), w)
            try:
                proc.stdin.write(fr.tobytes())
            except (BrokenPipeError, OSError):
                break
            if p == 0:
                Image.fromarray(fr).save(poster_path, optimize=True)
            if p in sheet_at:
                thumbs.append((p, p / a.fps, fr))
            if a.gif:
                gif_frames.append(resize_linear(fr, gw, gh))
            for k in (k0, k1):
                if k is not None and last_use.get(k) == p:
                    futures.pop(k, None)
    proc.stdin.close()
    err = proc.stderr.read().decode("utf-8", "replace")
    if proc.wait() != 0:
        raise SystemExit("ffmpeg failed: " + err.strip())
    contact_sheet(thumbs, W, H, sheet_path)

    info = probe_media(out)
    result = {
        "clip": str(out).replace("\\", "/"), "seconds": round(info["duration"], 3) if info["duration"] else None,
        "fps": info["fps"], "frames": info["frames"], "bytes": out.stat().st_size,
        "mb": round(out.stat().st_size / 1e6, 3), "size": [info["width"], info["height"]],
        "codec": info["codec"], "pix_fmt": info["pix_fmt"], "color": info["color"], "audio": info["audio"],
        "planned": {"frames": n_out, "seconds": round(seconds, 3), "sourceFrames": unique_frames, "fadeFrames": F,
                    "holdS": a.hold_last, "crf": a.crf},
        "takes": [{"name": t.name, "frames": len(t.frames), "seconds": round(t.duration, 3), "hud": t.hud,
                   "errors": len(t.errors)} for t in takes],
        "poster": str(poster_path).replace("\\", "/"), "sheet": str(sheet_path).replace("\\", "/"),
    }
    if a.gif:
        import gif_encoder
        gpath = resolve_out(a.gif, CLIPS)   # a fallback: kept out of description/ (verify_assets counts that folder)
        ginfo = gif_encoder.encode_with_budget(gif_encoder.ArrayFrames(gif_frames), str(gpath), a.gif_budget_mb,
                                               fps=a.fps, n_colors=255, dither=a.gif_dither, strength=0.5, thresh=4.0,
                                               hold_last=0.0, verbose=False)
        result["gif"] = {"path": str(gpath).replace("\\", "/"), "bytes": ginfo["bytes"], "mb": round(ginfo["bytes"] / 1e6, 3),
                         "size": [ginfo["w"], ginfo["h"]], "colors": ginfo["colors"], "thresh": ginfo["thresh"],
                         "gifFrames": ginfo["gif_frames"]}
    result["sec"] = round(time.time() - t_start, 1)
    print(json.dumps(result))
    return 0


if __name__ == "__main__":
    sys.exit(main())
