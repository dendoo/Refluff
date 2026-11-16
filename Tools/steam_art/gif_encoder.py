#!/usr/bin/env python3
"""
GIF fallback encoder for the Steam description clips (Docs/STORE_ART.md). The clips ship as MP4
(Tools/steam_art/make_clips.py); this module is only used by `make_clips.py --gif` when a GIF is needed after all
(Steam caps a GIF at 5 MB and all description media at 15 MB, so every byte counts here).

make_clips.py hands it finished frames (composited, cropped, downscaled, constant fps) through ArrayFrames +
encode_with_budget. The `encode` CLI below reads StoreGifApi take folders directly, but only for takes whose scene
and _ui layers have the same size (a 2560 x 1440 render); use make_clips.py --gif for anything else.

Pipeline (why each step):
  1. composite  : out = ui + (1 - ui.a) * scene in 8-bit sRGB, exactly like EconomyHud.DebugCapture.
  2. crop       : optional x,y,w,h in source pixels (region of interest).
  3. downscale  : Lanczos in LINEAR light (sRGB -> linear float, PIL 'F' per channel, -> sRGB). Gamma-space
                  downscaling darkens the bright fibre tips and the white tile grout.
  4. palette    : ONE global palette for the whole GIF (median cut on a mosaic of sampled frames + k-means polish).
                  A per-frame palette makes still areas flicker and defeats frame differencing.
  5. dither     : ORDERED, fixed to screen space (blue-noise-ish mask or Bayer 8x8), same offset on R,G,B (luma only).
                  A still pixel keeps its index from frame to frame. Floyd-Steinberg error diffusion "boils": one
                  changed pixel reshuffles the error of everything after it, every frame differs, and the file explodes.
  6. difference : a pixel is re-sent only when its SOURCE colour moved more than --thresh sRGB levels from the colour
                  it had when it was last sent (a reference per pixel, so slow drifts still land eventually).
                  Unsent pixels become the transparent index; the frame is cropped to the bounding box of what is
                  sent; disposal 1 (leave in place). A frame with nothing to send only lengthens the previous delay.
  7. budget     : if the file is over --budget-mb, retry with a higher threshold, then fewer colours.
Measured on still-camera footage: ~240 KB for the first frame, then ~1.1 B per re-sent pixel; a moving camera costs
160-240 KB per frame (a 4 s pan = 25 MB), so GIF takes need a still camera and ~10 % change per frame.

Usage:
  python Tools/steam_art/gif_encoder.py encode OUT.gif DIR [DIR ...] [--fps 25] [--width 616] [--crop x,y,w,h]
                                     [--colors 255] [--dither blue|bayer|none|fs] [--strength 0.5]
                                     [--thresh 4] [--hold-last 1.2] [--budget-mb 3.0] [--keep-every 1]
  python Tools/steam_art/gif_encoder.py selftest SCREENSHOT.jpg OUTDIR     (synthetic sequences + size table)
Needs numpy, Pillow, scipy; ffmpeg (imageio-ffmpeg) only for the selftest comparison.
"""
import argparse
import glob
import json
import os
import struct
import subprocess
import sys
import time

import numpy as np
from PIL import GifImagePlugin, Image
from scipy import ndimage
from scipy.spatial import cKDTree

TRANSPARENT = 255          # palette index reserved for "unchanged" pixels
KEY_RGB = (255, 0, 255)    # its colour; never displayed

# ----------------------------------------------------------------------------------------- colour helpers
_S2L = None


def srgb_to_linear_u8(a):
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
    """Lanczos in linear light, per channel in PIL 'F' mode."""
    if rgb_u8.shape[1] == w and rgb_u8.shape[0] == h:
        return rgb_u8
    lin = srgb_to_linear_u8(rgb_u8)
    out = np.empty((h, w, 3), np.float32)
    for c in range(3):
        out[..., c] = np.asarray(Image.fromarray(lin[..., c], "F").resize((w, h), Image.Resampling.LANCZOS))
    return linear_to_srgb_u8(out)


def composite_ui(scene_u8, ui_rgba_u8):
    a = ui_rgba_u8[..., 3:4].astype(np.float32) / 255.0
    out = ui_rgba_u8[..., :3].astype(np.float32) + scene_u8.astype(np.float32) * (1.0 - a)
    return np.clip(np.rint(out), 0, 255).astype(np.uint8)


# ----------------------------------------------------------------------------------------- frame source
class FolderFrames:
    """Numbered PNGs from one or more StoreGifApi folders, composited, cropped and downscaled, cached."""

    def __init__(self, dirs, width, crop=None, height=None, keep_every=1):
        """Delays come from each folder's seq.json (frameSteps x stepDt), so a take whose cadence changed mid-way
        (StoreGifApi.Every) keeps real time; keep_every=2 drops every other frame and doubles the delays."""
        self.paths = []
        self.durations_cs = []
        for d in dirs:
            ps = sorted(p for p in glob.glob(os.path.join(d, "f*.png")) if not p.endswith("_ui.png"))
            cs = [4] * len(ps)
            meta = os.path.join(d, "seq.json")
            if os.path.exists(meta):
                with open(meta, encoding="utf-8") as f:
                    m = json.load(f)
                st = m.get("frameSteps", [])
                sdt = float(m.get("stepDt", 0.02))
                if len(st) == len(ps) and len(st) > 1:
                    gaps = [st[i + 1] - st[i] for i in range(len(st) - 1)]
                    gaps.append(gaps[-1])
                    cs = [max(2, int(round(g * sdt * 100))) for g in gaps]
            if keep_every > 1:
                ps2, cs2 = [], []
                for i in range(0, len(ps), keep_every):
                    ps2.append(ps[i])
                    cs2.append(sum(cs[i:i + keep_every]))
                ps, cs = ps2, cs2
            self.paths.extend(ps)
            self.durations_cs.extend(cs)
        if not self.paths:
            raise SystemExit("no frames in " + ", ".join(dirs))
        self.crop = crop
        self.width = width
        first = self._load_src(0)
        sh, sw = first.shape[:2]
        self.height = height or int(round(sh * width / float(sw)))
        self.cache = {}

    def __len__(self):
        return len(self.paths)

    def _load_src(self, i):
        p = self.paths[i]
        rgb = np.asarray(Image.open(p).convert("RGB"))
        ui = p[:-4] + "_ui.png"
        if os.path.exists(ui):
            rgb = composite_ui(rgb, np.asarray(Image.open(ui).convert("RGBA")))
        if self.crop:
            x, y, w, h = self.crop
            rgb = rgb[y:y + h, x:x + w]
        return rgb

    def __getitem__(self, i):
        f = self.cache.get(i)
        if f is None:
            f = resize_linear(self._load_src(i), self.width, self.height)
            self.cache[i] = f
        return f


class ArrayFrames:
    def __init__(self, frames):
        self.frames = frames
        self.height, self.width = frames[0].shape[:2]

    def __len__(self):
        return len(self.frames)

    def __getitem__(self, i):
        return self.frames[i]


# ----------------------------------------------------------------------------------------- palette
def build_palette(frames, n_colors=255, samples=12, kmeans_iters=6, seed=1):
    n = len(frames)
    idx = sorted(set(np.linspace(0, n - 1, min(samples, n)).round().astype(int).tolist()))
    mosaic = np.concatenate([frames[i] for i in idx], axis=0)
    q = Image.fromarray(mosaic).quantize(colors=n_colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = np.array(q.getpalette()[:n_colors * 3], np.float32).reshape(-1, 3)
    used = np.unique(np.asarray(q))
    pal = pal[used]
    # k-means polish (Lloyd) on a pixel sample: median cut boxes are coarse in the dense browns of the pile
    rng = np.random.default_rng(seed)
    px = mosaic.reshape(-1, 3).astype(np.float32)
    if px.shape[0] > 200000:
        px = px[rng.choice(px.shape[0], 200000, replace=False)]
    for _ in range(kmeans_iters):
        lab = cKDTree(pal).query(px, workers=-1)[1]
        cnt = np.bincount(lab, minlength=len(pal)).astype(np.float32)
        for c in range(3):
            s = np.bincount(lab, weights=px[:, c], minlength=len(pal))
            pal[:, c] = np.where(cnt > 0, s / np.maximum(cnt, 1), pal[:, c])
    pal = np.clip(np.rint(pal), 0, 255)
    pal = np.unique(pal, axis=0)
    return pal.astype(np.float32)


# ----------------------------------------------------------------------------------------- dither masks
def bayer8():
    b = np.array([[0, 2], [3, 1]], np.float32)
    for _ in range(2):
        b = np.block([[4 * b + 0, 4 * b + 2], [4 * b + 3, 4 * b + 1]])
    return (b + 0.5) / 64.0 - 0.5


def blue_noise(size=64, seed=7):
    """Cheap blue-ish noise: white noise, high-passed twice, rank-normalised to uniform [-0.5, 0.5)."""
    rng = np.random.default_rng(seed)
    m = rng.random((size, size)).astype(np.float32)
    for _ in range(2):
        m = m - ndimage.gaussian_filter(m, 1.2, mode="wrap")
    r = np.empty(m.size, np.float32)
    r[np.argsort(m, axis=None)] = (np.arange(m.size) + 0.5) / m.size
    return r.reshape(size, size) - 0.5


def tiled(mask, h, w):
    ty = (h + mask.shape[0] - 1) // mask.shape[0]
    tx = (w + mask.shape[1] - 1) // mask.shape[1]
    return np.tile(mask, (ty, tx))[:h, :w]


# ----------------------------------------------------------------------------------------- encoder
def encode_gif(frames, out_path, fps=25.0, n_colors=255, dither="blue", strength=0.5, thresh=4.0,
               hold_last=1.0, dilate=1, palette=None, verbose=True):
    t0 = time.time()
    n = len(frames)
    H, W = frames.height, frames.width
    if palette is None:
        palette = build_palette(frames, n_colors)
    pal = palette[:255]
    tree = cKDTree(pal)
    # typical distance between neighbouring palette entries sets the dither amplitude
    nn = tree.query(pal, k=2)[0][:, 1]
    amp = float(np.median(nn)) * strength
    if dither == "bayer":
        mask = tiled(bayer8(), H, W)[..., None] * amp
    elif dither == "blue":
        mask = tiled(blue_noise(), H, W)[..., None] * amp
    else:
        mask = None

    def quantize(src):
        if dither == "fs":
            p = Image.new("P", (1, 1))
            flat = [int(v) for v in pal.reshape(-1)] + [0] * (768 - pal.size)
            p.putpalette(flat)
            q = Image.fromarray(src).quantize(palette=p, dither=Image.Dither.FLOYDSTEINBERG)
            return np.asarray(q).astype(np.uint8)
        x = src.astype(np.float32)
        if mask is not None:
            x = x + mask
        return tree.query(x.reshape(-1, 3), workers=-1)[1].astype(np.uint8).reshape(H, W)

    frame_cs = max(1, int(round(100.0 / fps)))
    durs = getattr(frames, "durations_cs", None)
    if not durs or len(durs) != n:
        durs = [frame_cs] * n
    gpal = np.zeros((256, 3), np.uint8)
    gpal[:len(pal)] = pal.astype(np.uint8)
    gpal[TRANSPARENT] = KEY_RGB

    chunks = []          # (sub_img, (x, y), cs, transparent?)
    shown = None
    ref = None
    sent_px = 0
    for i in range(n):
        src = frames[i]
        q = quantize(src)
        if shown is None:
            shown = q.copy()
            ref = src.astype(np.int16)
            chunks.append([q, (0, 0), durs[i], False])
            sent_px += q.size
            continue
        delta = np.abs(src.astype(np.int16) - ref).max(axis=2)
        changed = delta > thresh
        if dilate > 0 and changed.any():
            changed = ndimage.binary_dilation(changed, iterations=dilate)
        write = changed & (q != shown)
        ref[changed] = src[changed]
        if not write.any():
            chunks[-1][2] += durs[i]
            continue
        ys, xs = np.nonzero(write)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        sub = np.where(write[y0:y1, x0:x1], q[y0:y1, x0:x1], TRANSPARENT).astype(np.uint8)
        shown[write] = q[write]
        chunks.append([sub, (int(x0), int(y0)), durs[i], True])
        sent_px += int(write.sum())
    chunks[-1][2] += int(round(hold_last * 100))

    out = bytearray(b"GIF89a")
    out += struct.pack("<HHBBB", W, H, 0xF7, 0, 0)       # global colour table, 256 entries
    out += gpal.tobytes()
    out += b"!\xff\x0bNETSCAPE2.0\x03\x01" + struct.pack("<H", 0) + b"\x00"   # loop forever
    for sub, off, cs, transp in chunks:
        im = Image.fromarray(sub, "L")
        params = {"duration": cs * 10, "disposal": 1}
        if transp:
            params["transparency"] = TRANSPARENT
        out += b"".join(GifImagePlugin.getdata(im, offset=off, **params))
    out += b";"
    with open(out_path, "wb") as f:
        f.write(out)
    info = {"path": out_path.replace("\\", "/"), "bytes": len(out), "frames_in": n, "gif_frames": len(chunks),
            "w": W, "h": H, "colors": int(len(pal)), "dither": dither, "strength": strength, "thresh": thresh,
            "sent_share": round(sent_px / float(W * H * n), 4), "sec": round(time.time() - t0, 1)}
    if verbose:
        print(json.dumps(info))
    return info, palette


def gif_psnr(path, frames, every=10, fps=25.0):
    """Decodes the GIF with Pillow and compares every `every`-th displayed frame with the source frame shown at the
    same time (merged frames are mapped through the delays). sRGB PSNR in dB, and the decoded frame count."""
    im = Image.open(path)
    frame_cs = max(1, int(round(100.0 / fps)))
    vals = []
    k = 0
    t_cs = 0
    try:
        while True:
            src_i = int(round(t_cs / float(frame_cs)))
            if k % every == 0 and src_i < len(frames):
                a = np.asarray(im.convert("RGB")).astype(np.float32)
                b = frames[src_i].astype(np.float32)
                mse = float(((a - b) ** 2).mean())
                vals.append(99.0 if mse <= 1e-9 else 10 * np.log10(255.0 ** 2 / mse))
            t_cs += int(im.info.get("duration", frame_cs * 10)) // 10
            k += 1
            im.seek(im.tell() + 1)
    except EOFError:
        pass
    return (round(float(np.mean(vals)), 2) if vals else None), k


def encode_with_budget(frames, out_path, budget_mb, **kw):
    """Raise the difference threshold, then drop colours, until the file fits the budget."""
    palette = None
    tries = [(kw.get("thresh", 4.0), kw.get("n_colors", 255))]
    for t in (6.0, 8.0, 11.0):
        tries.append((t, kw.get("n_colors", 255)))
    tries += [(11.0, 160), (14.0, 128)]
    info = None
    for th, nc in tries:
        kw2 = dict(kw)
        kw2["thresh"] = th
        kw2["n_colors"] = nc
        pal = palette if nc == kw.get("n_colors", 255) else None
        info, pal_used = encode_gif(frames, out_path, palette=pal, **kw2)
        if nc == kw.get("n_colors", 255):
            palette = pal_used
        if info["bytes"] <= budget_mb * 1e6:     # MB as verify_assets.py counts them (1e6), not MiB
            break
    return info


# ----------------------------------------------------------------------------------------- self-test
def synth_sequences(shot_path, n=100):
    """Two synthetic 1232x694 -> 616x347 sequences cut from a store screenshot (2560x1440).
    pan    : the window slides 2 source px per frame (worst case: every pixel changes every frame).
    static : still camera; a 'tool' block walks down a band and leaves a brighter, more saturated 'clean' trail;
             plus +-1 level white noise everywhere (sim shimmer). Close to the real cleaning GIFs."""
    src = np.asarray(Image.open(shot_path).convert("RGB"))
    H, W = src.shape[:2]
    cw, ch = 1232, 694
    ow, oh = 616, 347
    pan = []
    for i in range(n):
        x = 300 + 2 * i
        y = 280 + i
        pan.append(resize_linear(src[y:y + ch, x:x + cw], ow, oh))
    base = src[(H - ch) // 2:(H - ch) // 2 + ch, (W - cw) // 2:(W - cw) // 2 + cw].astype(np.float32)
    lum = base.mean(axis=2, keepdims=True)
    clean = np.clip((base - lum) * 1.45 + lum * 1.12 + 6, 0, 255)
    rng = np.random.default_rng(3)
    static = []
    bx0, bx1 = 520, 760                    # band columns in the 1232 crop
    for i in range(n):
        f = base.copy()
        yb = int(40 + (ch - 120) * i / (n - 1))
        f[:yb, bx0:bx1] = clean[:yb, bx0:bx1]
        f[yb:yb + 40, bx0 - 20:bx1 + 20] = [205, 205, 210]          # the tool head
        f[yb + 10:yb + 30, bx0 + 60:bx1 - 60] = [150, 150, 158]
        f += rng.integers(-1, 2, size=f.shape[:2])[..., None]
        static.append(resize_linear(np.clip(f, 0, 255).astype(np.uint8), ow, oh))
    return {"pan": pan, "static": static}


def ffmpeg_exe():
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        return None


def selftest(shot, outdir):
    os.makedirs(outdir, exist_ok=True)
    seqs = synth_sequences(shot)
    rows = []
    for name, frames in seqs.items():
        fr = ArrayFrames(frames)
        pal = build_palette(fr, 255)
        variants = [("blue", 0.5, 4.0), ("blue", 0.5, 8.0), ("bayer", 0.5, 4.0), ("none", 0.0, 4.0), ("fs", 0.0, 4.0)]
        for dith, st, th in variants:
            out = os.path.join(outdir, "%s_%s_s%.1f_t%.0f.gif" % (name, dith, st, th))
            info, _ = encode_gif(fr, out, dither=dith, strength=st, thresh=th, palette=pal, verbose=False)
            info["psnr"], info["decoded_frames"] = gif_psnr(out, frames)
            info["seq"] = name
            rows.append(info)
            print(json.dumps(info))
        # frames for the ffmpeg comparison
        fdir = os.path.join(outdir, name + "_frames")
        os.makedirs(fdir, exist_ok=True)
        for i, f in enumerate(frames):
            Image.fromarray(f).save(os.path.join(fdir, "f%04d.png" % (i + 1)), compress_level=1)
        ff = ffmpeg_exe()
        if ff:
            for tag, vf in (("ff_bayer3", "split[a][b];[a]palettegen=max_colors=255:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=3:diff_mode=rectangle"),
                            ("ff_sierra", "split[a][b];[a]palettegen=max_colors=255:stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a:diff_mode=rectangle")):
                out = os.path.join(outdir, "%s_%s.gif" % (name, tag))
                subprocess.run([ff, "-v", "error", "-y", "-framerate", "25", "-i", os.path.join(fdir, "f%04d.png"),
                                "-filter_complex", vf, "-loop", "0", out], check=True)
                rows.append({"seq": name, "dither": tag, "bytes": os.path.getsize(out), "psnr": gif_psnr(out, frames)[0]})
                print(json.dumps(rows[-1]))
            for tag, args, ext in (("vp9_crf36", ["-c:v", "libvpx-vp9", "-crf", "36", "-b:v", "0", "-pix_fmt", "yuv420p"], "webm"),
                                   ("x264_crf22", ["-c:v", "libx264", "-crf", "22", "-preset", "slow", "-pix_fmt", "yuv420p"], "mp4")):
                out = os.path.join(outdir, "%s_%s.%s" % (name, tag, ext))
                subprocess.run([ff, "-v", "error", "-y", "-framerate", "25", "-i", os.path.join(fdir, "f%04d.png"),
                                "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2"] + args + [out], check=True)
                rows.append({"seq": name, "dither": tag, "bytes": os.path.getsize(out)})
                print(json.dumps(rows[-1]))
    with open(os.path.join(outdir, "selftest.json"), "w") as f:
        json.dump(rows, f, indent=1)
    return rows


# ----------------------------------------------------------------------------------------- CLI
def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    e = sub.add_parser("encode")
    e.add_argument("out")
    e.add_argument("dirs", nargs="+")
    e.add_argument("--fps", type=float, default=25.0)
    e.add_argument("--width", type=int, default=616)
    e.add_argument("--height", type=int, default=0)
    e.add_argument("--crop", default="")
    e.add_argument("--colors", type=int, default=255)
    e.add_argument("--dither", default="blue", choices=["blue", "bayer", "none", "fs"])
    e.add_argument("--strength", type=float, default=0.5)
    e.add_argument("--thresh", type=float, default=4.0)
    e.add_argument("--hold-last", type=float, default=1.0)
    e.add_argument("--budget-mb", type=float, default=3.0)
    e.add_argument("--keep-every", type=int, default=1, help="2 = half the frame rate (delays doubled)")
    s = sub.add_parser("selftest")
    s.add_argument("shot")
    s.add_argument("outdir")
    a = ap.parse_args()
    if a.cmd == "selftest":
        selftest(a.shot, a.outdir)
        return
    crop = tuple(int(v) for v in a.crop.split(",")) if a.crop else None
    frames = FolderFrames(a.dirs, a.width, crop, a.height or None, a.keep_every)
    info = encode_with_budget(frames, a.out, a.budget_mb, fps=a.fps, n_colors=min(a.colors, 255), dither=a.dither,
                              strength=a.strength, thresh=a.thresh, hold_last=a.hold_last)
    info["psnr"], info["decoded_frames"] = gif_psnr(a.out, frames)
    print(json.dumps(info))


if __name__ == "__main__":
    main()
