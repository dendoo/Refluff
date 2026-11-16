"""Trailer editor (Docs/TRAILER.md 3, 4, 6, 7, 9): EDL -> 1920x1080 60 fps CFR H.264 master with an AAC mix.

    python Tools/steam_art/trailer/make_trailer.py [--edl EDL] [--out OUT.mp4] [--only 01,02,...] [--preview]
                                                   [--frames-at T1,T2,...] [--no-audio] [--jobs N] [--date TEXT]

EDL = Tools/steam_art/trailer/edl.json by default (schema: README_editor.md). OUT = SteamStore/trailer/refluff_trailer_main.mp4.
Writes OUT, OUT's cover (<base>_cover.png, 1920x1080, the frame at the EDL's cover time, no text) and contact sheet
(<base>_sheet.jpg, one frame per shot with timecodes), where <base> = OUT's stem without "_main"; audio stems + the mix
go to SteamStore/_work/trailer/stems/ for the default OUT and to <base>_stems/ next to any other OUT; a render report
<base>_render.json (shots, audio sources, warnings, timings) goes next to the stems. Prints one JSON line.

Per output frame k (time T = k / fps on the trailer timeline):
  1. shot      the shot whose [round(start*fps), round((start+dur)*fps)) covers k; shot-local time tl = frames since its
               cut / fps; take time tt = in + tl * speed (speed 0 = a freeze on `in`)
  2. frame     the take frame shown by its seq.json timing: the last frame whose time <= the take time of the middle
               of the output frame (60 fps takes map 1:1 even with a rounded stepDt, 25 fps placeholders hold); past
               the take's end the last frame holds (warns unless "hold_last")
  3. camera    the cam rect at tl (keyframes eased, centre linear / size geometric like clip 5's virtual camera, every key
               forced to 16:9 inside itself and kept inside the frame): the integer box around it (+ the filter support)
               is cut out, turned linear, and Lanczos-resampled with the sub-pixel box straight to the output size
  4. HUD       "hud": the take's premultiplied fNNNN_ui.png panel gets the same rect (scaled to the panel's pixels), is
               resampled as premultiplied RGBa and laid over in 8-bit sRGB (over = ui + (1 - a) * scene, make_clips.py)
  5. fx        wipe (a second take, same camera, edge line), grade / logo_whip / endcard background darken + blur
  6. overlays  logo whip (#03), end card (#21), captions (titles.py: on the cut, 0.15 s 0.92 -> 1 scale + alpha)
Frames are rendered on a thread pool (PIL decode/resample and numpy release the GIL) and streamed as raw RGB to one
ffmpeg process (no temp PNGs), which also muxes the normalised mix: libx264 High, yuv420p, BT.709 tv range tagged,
-b:v 20M -maxrate 24M -bufsize 40M (or --crf N with the same cap), 60 fps CFR, GOP 2 s, +faststart; AAC-LC 192k 48 kHz.
Audio: per shot foley.render_take(take_dir, t0, t1, sr) (other lane; missing module / no audio.jsonl = silence) + the
shot's extra wavs + optional music (ducked out on "duck_music" shots) -> 8 ms fades at cuts unless "carry" -> master
(tail: silent for the last 0.5 s) -> lookahead limiter only when the -14 LUFS gain would push the true peak over -1 dBTP
-> two-pass ffmpeg loudnorm I=-14 TP=-1 linear=true (LRA as is) -> 48 kHz float WAV -> AAC.
"""
import argparse
import bisect
import json
import math
import os
import re
import subprocess
import sys
import threading
import time
from collections import OrderedDict
from concurrent.futures import Future, ThreadPoolExecutor
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE))
import make_clips as MC  # noqa: E402
import titles as TT  # noqa: E402
from clip_probe import ffmpeg_exe, probe_media  # noqa: E402

ROOT = HERE.parents[2]
STORE = ROOT / "SteamStore"
CLIPS = STORE / "_work" / "clips"
WORK = STORE / "_work" / "trailer"
DEFAULT_EDL = HERE / "edl.json"
DEFAULT_OUT = STORE / "trailer" / "refluff_trailer_main.mp4"
BAR = 4 * 60.0 / 90.0                    # 90 BPM, 4/4: one bar = 2.6667 s (160 frames at 60 fps)
ASPECT = 16.0 / 9.0                      # the output's width / height; Edl() sets it from the EDL's "size" (9:16 = TikTok cut)
SR = 48000
_warned = set()
_warn_lock = threading.Lock()
WARNINGS = []


def warn(msg, key=None):
    with _warn_lock:
        k = key or msg
        if k in _warned:
            return
        _warned.add(k)
        WARNINGS.append(msg)
    print("warning: " + msg, file=sys.stderr)


def rel(p):
    try:
        return str(Path(p).resolve().relative_to(ROOT)).replace("\\", "/")
    except ValueError:
        return str(p).replace("\\", "/")


# ------------------------------------------------------------------------------------------------ easing
def _ease(name, s):
    s = min(max(s, 0.0), 1.0)
    if name in ("linear", None):
        return s
    if name == "smootherstep":
        return s * s * s * (s * (s * 6 - 15) + 10)
    if name in ("easeInOut", "easeInOutCubic"):
        return 4 * s * s * s if s < 0.5 else 1 - (-2 * s + 2) ** 3 / 2
    if name in ("easeOut", "easeOutCubic"):
        return 1 - (1 - s) ** 3
    if name in ("easeIn", "easeInCubic"):
        return s * s * s
    if name == "hold":
        return 0.0 if s < 1.0 else 1.0
    return s * s * (3 - 2 * s)             # smoothstep (default)


def parse_time(v, what="time"):
    """Seconds from a number, "m:ss.sss", or "Nb" (N bars on the 90 BPM grid)."""
    if isinstance(v, (int, float)):
        return float(v)
    if isinstance(v, str):
        s = v.strip()
        if s.endswith("b"):
            return float(s[:-1]) * BAR
        m = re.match(r"^(\d+):(\d+(?:\.\d+)?)$", s)
        if m:
            return int(m.group(1)) * 60 + float(m.group(2))
        return float(s)
    raise ValueError("bad %s: %r" % (what, v))


# ------------------------------------------------------------------------------------------------ caches
class OnceCache:
    """Thread-safe compute-once LRU: get(key, fn) runs fn() once per key; concurrent callers wait on its Future."""

    def __init__(self, maxsize):
        self.max, self.d, self.lock = max(1, maxsize), OrderedDict(), threading.Lock()
        self.hits = self.misses = 0

    def get(self, key, fn):
        with self.lock:
            f = self.d.get(key)
            owner = f is None
            if owner:
                f = Future()
                self.d[key] = f
                self.misses += 1
                while len(self.d) > self.max:
                    self.d.popitem(last=False)
            else:
                self.d.move_to_end(key)
                self.hits += 1
        if owner:
            try:
                f.set_result(fn())
            except BaseException as e:
                f.set_exception(e)
                with self.lock:
                    if self.d.get(key) is f:
                        del self.d[key]
        return f.result()


DECODE = OnceCache(12)
BACKGROUND = OnceCache(12)


def load_rgb(path):
    return DECODE.get(("rgb", str(path)), lambda: np.asarray(Image.open(path).convert("RGB")))


def load_rgba(path):
    return DECODE.get(("rgba", str(path)), lambda: np.asarray(Image.open(path).convert("RGBA")))


# ------------------------------------------------------------------------------------------------ sources
IMG_EXT = {".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tif", ".tiff"}


class Source:
    """A StoreGifApi take folder (make_clips.Take timing) or a single still image (held forever)."""

    def __init__(self, path):
        self.path = path
        if path.is_file():
            self.kind, self.name, self.dir = "still", path.stem, None
            self.frames, self.times, self.duration, self.seq = [path], [0.0], float("inf"), {}
            self.hud = (path.with_name(path.stem + "_ui.png")).exists()
        else:
            tk = MC.Take(str(path))
            self.kind, self.name, self.dir = "take", tk.name, tk.dir
            self.frames, self.times, self.duration, self.seq = tk.frames, tk.times, tk.duration, tk.seq
            self.hud = tk.hud
        # make_clips.Take times count from the first captured frame (its Step = frameSteps[n - 1]), foley.py's from
        # SeqBegin (Step s at s * stepDt, the frame captured right after Step s): foley time = take time + t_origin
        self.t_origin = 0.0
        if self.kind == "take":
            m = MC.FRAME_RE.match(self.frames[0].name)
            n = int(m.group(1)) if m else 1
            fs = self.seq.get("frameSteps") or []
            step0 = int(fs[n - 1]) if 0 < n <= len(fs) else 2 * n
            self.t_origin = step0 * float(self.seq.get("stepDt", 0.02))
        self._size = None
        n = len(self.times)
        self.fps = (n - 1) / self.times[-1] if n > 1 and self.times[-1] > 0 else 0.0

    def size(self):
        if self._size is None:
            with Image.open(self.frames[0]) as im:
                self._size = im.size
        return self._size

    def index_at(self, tt):
        """(frame index shown at take time tt, past the end?)"""
        i = bisect.bisect_right(self.times, tt + 1e-4) - 1
        return min(max(i, 0), len(self.frames) - 1), tt > self.duration + 1e-6

    def ui_path(self, i):
        q = self.frames[i]
        u = q.with_name(q.stem + "_ui.png")
        return u if u.exists() else None

    def has_audio_log(self):
        return self.dir is not None and (self.dir / "audio.jsonl").exists()


_sources = {}


def resolve_source(spec, where=""):
    if not spec:
        raise SystemExit("%s: no take" % where)
    p = Path(spec)
    cands = [p] if p.is_absolute() else [ROOT / p, CLIPS / p, HERE / p, Path.cwd() / p]
    for c in cands:
        if c.is_dir() or (c.is_file() and c.suffix.lower() in IMG_EXT):
            key = str(c.resolve())
            if key not in _sources:
                _sources[key] = Source(c.resolve())
            return _sources[key]
    raise SystemExit("%s: take %r not found (looked in %s)" % (where, spec, ", ".join(rel(c) for c in cands)))


# ------------------------------------------------------------------------------------------------ EDL
def fit_rect(rect, sw, sh):
    """The largest output-aspect (ASPECT) rect centred inside `rect`, then scaled / shifted to lie inside the sw x sh frame."""
    x, y, w, h = [float(v) for v in rect]
    if w <= 1 or h <= 1:
        raise ValueError("cam rect too small: %r" % (rect,))
    if w / h > ASPECT:
        nw = h * ASPECT
        x, w = x + (w - nw) / 2, nw
    else:
        nh = w / ASPECT
        y, h = y + (h - nh) / 2, nh
    s = min(1.0, sw / w, sh / h)
    if s < 1.0:
        cx, cy = x + w / 2, y + h / 2
        w, h = w * s, h * s
        x, y = cx - w / 2, cy - h / 2
    return (min(max(x, 0.0), sw - w), min(max(y, 0.0), sh - h), w, h)


def norm_fx(v):
    out = []
    for f in (v if isinstance(v, list) else ([v] if v else [])):
        out.append({"type": f} if isinstance(f, str) else dict(f))
    return {f["type"]: f for f in out}


class Shot:
    def __init__(self, d, idx, fps):
        self.raw = d
        self.id = str(d.get("id", "%02d" % (idx + 1)))
        where = "shot " + self.id
        if "bars" in d:
            b0, bl = d["bars"]
            self.start, self.dur = float(b0) * BAR, float(bl) * BAR
        else:
            self.start, self.dur = parse_time(d["start"], where), parse_time(d["dur"], where)
        self.k0, self.k1 = int(round(self.start * fps)), int(round((self.start + self.dur) * fps))
        self.half = 0.5 / fps               # take frames are picked at the middle of each output frame
        self.src = resolve_source(d.get("take"), where)
        self.in_ = parse_time(d.get("in", 0.0), where)
        self.speed = float(d.get("speed", 1.0))
        self.hud = bool(d.get("hud", False))
        self.fx = norm_fx(d.get("fx"))
        if "wipe" in self.fx:
            w = self.fx["wipe"]
            w["src"] = resolve_source(w.get("take"), where + " wipe")
            w["in_"] = parse_time(w.get("in", 0.0), where)
        self.cam_clock = d.get("cam_clock", "shot")
        self.cam = []
        for key in d.get("cam") or []:
            self.cam.append((parse_time(key.get("t", 0.0)), [float(v) for v in key["rect"]], key.get("ease", "smoothstep")))
        self.cam.sort(key=lambda c: c[0])
        ts = d.get("titles") or ([d["title"]] if d.get("title") else [])
        self.titles = [dict(t) for t in ts]
        self.audio = dict(d.get("audio") or {})
        if self.hud and not self.src.hud and self.src.ui_path(0) is None:
            warn("%s: hud requested but %s has no _ui.png panels (scene only)" % (where, self.src.name))

    def take_time(self, tl):
        return self.in_ + tl * self.speed

    def sample_time(self, tl, speed=None):
        """Take time whose frame output frame tl shows: mid-interval, so a 60 fps take whose seq.json stepDt is rounded
        (0.016667) cannot drift into duplicated / dropped frames, and 25 fps takes hold like make_clips.py."""
        sp = self.speed if speed is None else speed
        return (tl + self.half) * sp

    def cam_rect(self, tl, src):
        sw, sh = src.size()
        if not self.cam:
            return fit_rect((0, 0, sw, sh), sw, sh)
        t = tl if self.cam_clock == "shot" else self.take_time(tl)
        keys = self.cam
        if t <= keys[0][0] or len(keys) == 1:
            return fit_rect(keys[0][1], sw, sh)
        for (t0, a, _), (t1, b, ease) in zip(keys, keys[1:]):
            if t <= t1:
                a, b = fit_rect(a, sw, sh), fit_rect(b, sw, sh)
                if a == b or t1 <= t0:
                    return b
                s = _ease(ease, (t - t0) / (t1 - t0))
                w = a[2] * math.pow(b[2] / a[2], s)
                h = a[3] * math.pow(b[3] / a[3], s)
                cx = (a[0] + a[2] / 2) * (1 - s) + (b[0] + b[2] / 2) * s
                cy = (a[1] + a[3] / 2) * (1 - s) + (b[1] + b[3] / 2) * s
                return fit_rect((cx - w / 2, cy - h / 2, w, h), sw, sh)
        return fit_rect(keys[-1][1], sw, sh)


class Edl:
    def __init__(self, path, fps=None):
        self.path = Path(path)
        d = json.loads(self.path.read_text(encoding="utf-8"))
        self.raw = d
        self.fps = float(fps or d.get("fps", 60))
        self.size = tuple(d.get("size", [1920, 1080]))
        global ASPECT
        ASPECT = self.size[0] / float(self.size[1])
        self.shots = [Shot(s, i, self.fps) for i, s in enumerate(d["shots"])]
        self.shots.sort(key=lambda s: s.k0)
        ids = [s.id for s in self.shots]
        if len(set(ids)) != len(ids):
            raise SystemExit("duplicate shot ids in %s" % path)
        for a, b in zip(self.shots, self.shots[1:]):
            if b.k0 > a.k1:
                warn("gap between shots %s and %s (%.3f s): black frames" % (a.id, b.id, (b.k0 - a.k1) / self.fps))
            elif b.k0 < a.k1:
                warn("shots %s and %s overlap by %.3f s: the later one wins" % (a.id, b.id, (a.k1 - b.k0) / self.fps))
        self.n = max(s.k1 for s in self.shots)
        self._k0 = [s.k0 for s in self.shots]
        self.titles = []            # (f0, f1, spec, shot)
        for s in self.shots:
            for t in s.titles:
                f0 = s.k0 + int(round(parse_time(t.get("at", 0.0)) * self.fps))
                f1 = f0 + int(round(parse_time(t["dur"]) * self.fps)) if t.get("dur") is not None else s.k1
                self.titles.append((f0, f1, t, s))
        self.music = d.get("music") or None
        self.voice = d.get("voice") or None
        self.audio = dict({"sr": SR, "tail_silence": 0.5, "tail_fade": 0.6, "cut_fade_ms": 8.0, "lufs": -14.0,
                           "tp": -1.0}, **(d.get("audio") or {}))
        self.cover_t = parse_time((d.get("cover") or {}).get("t", 1.5))
        self.date = d.get("date")

    def shot_at(self, k):
        j = bisect.bisect_right(self._k0, k) - 1
        if j < 0:
            return None
        s = self.shots[j]
        return s if k < s.k1 else None

    def by_id(self, sid):
        for s in self.shots:
            if s.id == sid:
                return s
        return None


# ------------------------------------------------------------------------------------------------ pixels
_L2S = None
_L2S_N = 1 << 16


def lin_to_srgb_u8(f):
    """float linear -> uint8 sRGB through a 65536-entry table (within 1 level of make_clips.linear_to_srgb_u8, ~4x faster)."""
    global _L2S
    if _L2S is None:
        x = np.arange(_L2S_N, dtype=np.float64) / (_L2S_N - 1)
        s = np.where(x <= 0.0031308, x * 12.92, 1.055 * np.power(x, 1.0 / 2.4) - 0.055)
        _L2S = np.clip(np.rint(s * 255.0), 0, 255).astype(np.uint8)
    idx = np.clip(f, 0.0, 1.0)
    idx *= (_L2S_N - 1)
    idx += 0.5
    return _L2S[idx.astype(np.uint16)]


def _box(shape, rect, W, H):
    sh, sw = shape[:2]
    x, y, w, h = rect
    m = int(math.ceil(3.0 * max(w / W, h / H, 1.0))) + 2          # Lanczos-3 support at this scale, + slack
    bx0, by0 = max(0, int(math.floor(x)) - m), max(0, int(math.floor(y)) - m)
    bx1, by1 = min(sw, int(math.ceil(x + w)) + m), min(sh, int(math.ceil(y + h)) + m)
    return (bx0, by0, bx1, by1), (x - bx0, y - by0, x - bx0 + w, y - by0 + h)


def resample_linear(img, rect, W, H):
    """uint8 sRGB image, sub-pixel rect -> float32 linear W x H (crop the integer box, Lanczos in linear light)."""
    (bx0, by0, bx1, by1), box = _box(img.shape, rect, W, H)
    sub = img[by0:by1, bx0:bx1]
    out = np.empty((H, W, 3), np.float32)
    for c in range(3):
        ch = MC.srgb_u8_to_linear(sub[..., c])                       # table lookup -> contiguous float32
        out[..., c] = np.asarray(Image.fromarray(ch, "F").resize((W, H), Image.Resampling.LANCZOS, box=box))
    return out


def resample_premult(ui, rect, W, H):
    """uint8 premultiplied RGBA panel -> W x H premultiplied RGBA (PIL 'RGBa' resize: no second premultiply)."""
    (bx0, by0, bx1, by1), box = _box(ui.shape, rect, W, H)
    sub = np.ascontiguousarray(ui[by0:by1, bx0:bx1])
    im = Image.frombytes("RGBa", (sub.shape[1], sub.shape[0]), sub.tobytes())
    return np.asarray(im.resize((W, H), Image.Resampling.LANCZOS, box=box))


def render_view(src, i, rect, W, H, hud):
    scene = load_rgb(src.frames[i])
    out = lin_to_srgb_u8(resample_linear(scene, rect, W, H))
    if hud:
        up = src.ui_path(i)
        if up is None:
            warn("%s frame %d has no _ui.png: scene only" % (src.name, i + 1), ("noui", src.name))
        else:
            ui = load_rgba(up)
            sx, sy = ui.shape[1] / float(scene.shape[1]), ui.shape[0] / float(scene.shape[0])
            out = MC.composite_ui(out, resample_premult(ui, (rect[0] * sx, rect[1] * sy, rect[2] * sx, rect[3] * sy), W, H))
    return out


DARK = np.array([8, 10, 14], np.float32)
GRADES = {"logo_whip": (0.30, 3.0, 0.25), "endcard": (0.45, 7.5, 0.60), "grade": (0.0, 0.0, 0.0)}


def bg_grade(shot, tl, k):
    """(darken 0..1, blur px) of the shot's background at tl: logo whip -30 % + light blur, end card -45 % + blur."""
    for name, (d0, b0, r0) in GRADES.items():
        f = shot.fx.get(name)
        if f:
            r = float(f.get("ramp", r0))
            e = TT.smoothstep(tl / r) if r > 0 else 1.0
            return float(f.get("darken", d0)) * e, float(f.get("blur", b0)) * e * k
    return 0.0, 0.0


def wipe_state(shot, tl, W):
    w = shot.fx["wipe"]
    t0, t1 = parse_time(w.get("t0", 0.3)), parse_time(w.get("t1", shot.dur - 0.5))
    feather = float(w.get("feather", 24)) * W / 1920.0
    u = _ease(w.get("ease", "smoothstep"), (tl - t0) / max(1e-6, t1 - t0))
    return -feather + u * (W + 2 * feather), feather


def apply_wipe(a, b, p, feather, line, k, rtl=False):
    """Left of the edge shows b (the clean state), right of it a; a soft light line rides the edge."""
    W = a.shape[1]
    xs = np.arange(W, dtype=np.float32) + 0.5
    if rtl:
        xs = W - xs
    m = np.clip((p - xs) / max(1.0, feather) + 0.5, 0.0, 1.0)
    m = (m * m * (3 - 2 * m))[None, :, None]
    out = a.astype(np.float32) * (1 - m) + b.astype(np.float32) * m
    if line and -feather < p < W + feather:
        g = 0.75 * np.exp(-((xs - p) / (1.6 * k)) ** 2) + 0.22 * np.exp(-((xs - p) / (16.0 * k)) ** 2)
        out += (255.0 - out) * g[None, :, None]
    return np.clip(out + 0.5, 0, 255).astype(np.uint8)


def background(shot, tl, W, H):
    """The shot's picture at tl before overlays (cached: a held 25 fps frame under a still camera renders once)."""
    src = shot.src
    i, past = src.index_at(shot.in_ + shot.sample_time(tl))
    if past and "hold_last" not in shot.fx:
        warn("shot %s: take %s ends at %.2f s, the shot wants %.2f s: last frame held (add fx hold_last)"
             % (shot.id, src.name, src.duration, shot.take_time(shot.dur)), ("past", shot.id))
    rect = shot.cam_rect(tl, src)
    k = min(W, H) / 1080.0
    wkey, wipe = None, shot.fx.get("wipe")
    if wipe:
        sb = wipe["src"]
        ib, _ = sb.index_at(wipe["in_"] + shot.sample_time(tl, float(wipe.get("speed", shot.speed))))
        p, feather = wipe_state(shot, tl, W)
        wkey = (ib, round(p, 2))
    darken, blur = bg_grade(shot, tl, k)
    key = (shot.id, i, tuple(round(v, 3) for v in rect), wkey, round(darken, 3), round(blur, 2), W, H)

    def make():
        img = render_view(src, i, rect, W, H, shot.hud)
        if wipe:
            if p <= -feather:
                pass
            else:
                imb = render_view(sb, ib, shot.cam_rect(tl, sb), W, H, bool(wipe.get("hud", shot.hud)))
                img = imb if p >= W + feather and not wipe.get("line", True) else \
                    apply_wipe(img, imb, p, feather, wipe.get("line", True), k, wipe.get("dir") == "rtl")
        if blur > 0.05:
            img = np.asarray(Image.fromarray(img).filter(ImageFilter.GaussianBlur(blur)))
        if darken > 0.001:
            img = (img.astype(np.float32) * (1 - darken) + DARK * darken + 0.5).astype(np.uint8)
        return img

    return BACKGROUND.get(key, make)


def render_frame(edl, k, W, H, overlays=True, date=None):
    """Finished output frame k (uint8 H x W x 3): background, then the logo whip / end card, then the captions."""
    shot = edl.shot_at(k)
    if shot is None:
        return np.zeros((H, W, 3), np.uint8)
    tl = (k - shot.k0) / edl.fps
    frame = np.array(background(shot, tl, W, H))          # a copy: cached backgrounds stay clean
    if not overlays:
        return frame
    ctx = {}
    if "logo_whip" in shot.fx:
        TT.draw_logo_whip(frame, tl, shot.fx["logo_whip"], ctx)
    if "endcard" in shot.fx:
        cfg = {a: b for a, b in shot.fx["endcard"].items() if a != "type"}
        if date is not None:
            cfg["date"] = date
        elif edl.date and "date" not in cfg:
            cfg["date"] = edl.date
        TT.draw_endcard(frame, tl, cfg)
    for f0, f1, spec, s in edl.titles:
        if f0 <= k < f1:
            TT.draw_title(frame, spec, (k - f0) / edl.fps, ctx)
    return frame


# ------------------------------------------------------------------------------------------------ audio
def write_wav_f32(path, x, sr):
    """Stereo float32 WAV (WAVE_FORMAT_IEEE_FLOAT), numpy only."""
    import struct
    x = np.ascontiguousarray(np.asarray(x, np.float32).reshape(-1, 2))
    data = x.tobytes()
    with open(path, "wb") as f:
        f.write(b"RIFF" + struct.pack("<I", 36 + len(data)) + b"WAVE")
        f.write(b"fmt " + struct.pack("<IHHIIHH", 16, 3, 2, sr, sr * 8, 8, 32))
        f.write(b"data" + struct.pack("<I", len(data)) + data)


_decoded = {}


def decode_audio(path, sr, cache=True):
    """Any audio file -> float32 (n, 2) at sr, through ffmpeg (cached unless cache=False)."""
    key = (str(path), sr)
    if not cache:
        _decoded.pop(key, None)
    if key not in _decoded:
        r = subprocess.run([ffmpeg_exe(), "-v", "error", "-i", str(path), "-f", "f32le", "-ac", "2", "-ar", str(sr), "-"],
                           capture_output=True)
        if r.returncode != 0:
            raise SystemExit("cannot decode %s: %s" % (path, r.stderr.decode("utf-8", "replace").strip()))
        _decoded[key] = np.frombuffer(r.stdout, np.float32).reshape(-1, 2).copy()
    return _decoded[key]


def resolve_file(p):
    q = Path(p)
    for c in ([q] if q.is_absolute() else [ROOT / q, HERE / q, Path.cwd() / q]):
        if c.is_file():
            return c
    raise SystemExit("audio file not found: %s" % p)


def ramp(n_total, n_in, n_out):
    """Gain curve with raised-cosine fade in / out (sample counts)."""
    g = np.ones(n_total, np.float32)
    if n_in > 0:
        n = min(n_in, n_total)
        g[:n] *= 0.5 - 0.5 * np.cos(np.pi * (np.arange(n) + 0.5) / n_in)
    if n_out > 0:
        n = min(n_out, n_total)
        g[n_total - n:] *= (0.5 + 0.5 * np.cos(np.pi * (np.arange(n_out - n, n_out) + 0.5) / n_out))
    return g


def _add(bus, at, x):
    n = bus.shape[0]
    if at >= n or at + x.shape[0] <= 0:
        return
    a0, x0 = max(0, at), max(0, -at)
    m = min(n - a0, x.shape[0] - x0)
    bus[a0:a0 + m] += x[x0:x0 + m]


def _fit_len(x, L):
    x = np.asarray(x, np.float32)
    if x.ndim == 1:
        x = np.stack([x, x], 1)
    if x.shape[0] >= L:
        return x[:L]
    return np.concatenate([x, np.zeros((L - x.shape[0], 2), np.float32)])


def _foley():
    try:
        import foley  # noqa: F401  (Tools/steam_art/trailer/foley.py, the foley lane)
        return foley
    except ImportError:
        return None
    except Exception as e:  # a broken foley.py must not stop a picture review
        warn("foley.py failed to import (%s): take audio is silent" % e)
        return None


def _take_audio(fo, s, sr, s0, s1, cut, sources, notes):
    """One shot's foley segment (placed by the caller), or None."""
    a = s.audio
    carry = a.get("carry", 0.0)
    carry = 0.5 if carry is True else parse_time(carry or 0.0)
    if not s.src.has_audio_log():
        notes.append("shot %s: %s has no audio.jsonl (silent)" % (s.id, s.src.name))
        return None
    L = s1 - s0 + int(carry * sr)
    sp = s.speed if s.speed > 0 else 1.0
    t0 = s.in_ + s.src.t_origin          # foley time (0 = SeqBegin) of the picture's take time `in`
    try:
        seg = fo.render_take(s.src.dir, t0, t0 + L / float(sr) * sp, sr=sr)
    except Exception as e:
        warn("shot %s: foley.render_take failed (%s): silent" % (s.id, e))
        return None
    if seg is None:
        return None
    seg = np.asarray(seg, np.float32)
    if seg.ndim == 1:
        seg = np.stack([seg, seg], 1)
    if abs(sp - 1.0) > 1e-6:           # a retimed shot: its audio is resampled (the pitch follows), warned
        warn("shot %s: speed %.3f resamples its take audio (pitch shifts)" % (s.id, sp))
        src_t = np.arange(L) * sp
        seg = np.stack([np.interp(src_t, np.arange(seg.shape[0]), seg[:, c]) for c in range(2)], 1)
    fade_out = int(carry * sr) if carry > 0 else cut
    seg = _fit_len(seg, L) * ramp(L, 0 if a.get("carry_in") else cut, fade_out)[:, None]
    used = getattr(fo, "last_sources", None)
    if not isinstance(used, (list, tuple)):
        try:                             # foley.build_plan is cached: every clip the take plays (a superset of [t0, t1])
            used = sorted({str(v.clip) for v in fo.build_plan(s.src.dir, sr).voices})
        except Exception:
            used = None
    sources.append({"shot": s.id, "take": rel(s.src.dir), "t0": round(t0, 4), "t1": round(t0 + L / float(sr) * sp, 4),
                    "files": list(used) if used is not None else None})
    return seg * (10 ** (float(a.get("gain_db", 0.0)) / 20))


def _music(edl, sr, n, sources):
    m = edl.music
    path = resolve_file(m["wav"])
    x = decode_audio(path, sr)
    x = x[int(parse_time(m.get("in", 0.0)) * sr):]
    x = x * ramp(x.shape[0], int(parse_time(m.get("fade_in", 0.02)) * sr), 0)[:, None]
    mus = np.zeros((n, 2), np.float32)
    _add(mus, int(round(parse_time(m.get("start", 0.0)) * sr)), x * (10 ** (float(m.get("gain_db", -16.0)) / 20)))
    # duck_music shots (TRAILER.md 7: out in #11/#12, back at #13): a target gain per sample, smoothed by a short ramp
    duck = 10 ** (float(m["duck_db"]) / 20) if m.get("duck_db") is not None else 0.0
    env = np.ones(n, np.float32)
    for s in edl.shots:
        if s.audio.get("duck_music"):
            env[int(round(s.k0 / edl.fps * sr)):int(round(s.k1 / edl.fps * sr))] = duck
    rn = max(1, int(parse_time(m.get("ramp", 0.06)) * sr / 2))
    c = np.cumsum(np.concatenate([np.full(rn, env[0]), env, np.full(rn, env[-1])]).astype(np.float64))
    c = np.concatenate([[0.0], c])
    env = ((c[2 * rn:2 * rn + n] - c[:n]) / (2 * rn)).astype(np.float32)
    mus *= env[:, None]
    fo_n = min(n, int(parse_time(m.get("fade_out", 1.5)) * sr))
    if fo_n > 0:
        mus[-fo_n:] *= ramp(fo_n, 0, fo_n)[:, None]
    sources.append({"music": rel(path)})
    return mus


def _voice(edl, sr, n, sources):
    """The voiceover track and the duck envelope it puts on every other bus: where the voice's 20 ms RMS is above
    threshold_db the rest drops to duck_db, with attack / release ramps (a speech gap shorter than hold_s stays ducked)."""
    v = edl.voice
    path = resolve_file(v["wav"])
    x = decode_audio(path, sr, cache=False)
    x = x[int(parse_time(v.get("in", 0.0)) * sr):]
    voc = np.zeros((n, 2), np.float32)
    _add(voc, int(round(parse_time(v.get("start", 0.0)) * sr)), x * (10 ** (float(v.get("gain_db", 0.0)) / 20)))
    win = max(1, int(0.02 * sr))
    m = n // win
    rms = np.sqrt(np.mean(np.square(voc[:m * win].mean(axis=1)).reshape(m, win), axis=1) + 1e-12)
    on = 20 * np.log10(rms) > float(v.get("threshold_db", -42.0))
    hold = int(float(v.get("hold_s", 0.35)) / 0.02)
    idx = np.flatnonzero(on)
    for a, b in zip(idx, idx[1:]):
        if 1 < b - a <= hold:
            on[a:b] = True
    duck = 10 ** (float(v.get("duck_db", -8.0)) / 20)
    target = np.repeat(np.where(on, duck, 1.0), win).astype(np.float32)
    target = np.concatenate([target, np.full(n - target.shape[0], target[-1] if target.size else 1.0, np.float32)])
    att, rel_ = math.exp(-1.0 / (float(v.get("attack", 0.06)) * sr)), math.exp(-1.0 / (float(v.get("release", 0.30)) * sr))
    env = np.empty(n, np.float32)
    g = 1.0
    step = 64                                         # one-pole follower at 64-sample resolution (the ramps are 60+ ms)
    for i in range(0, n, step):
        t = float(target[i])
        c = att if t < g else rel_
        g = t + (g - t) * c ** step
        env[i:i + step] = g
    sources.append({"voice": rel(path)})
    return voc, env


def build_mix(edl, sr):
    """Stems {foley, sfx, music, voice} and the master (float32 (n, 2)) on the full timeline, plus the audio sources used."""
    n = int(round(edl.n / edl.fps * sr))
    stems = {b: np.zeros((n, 2), np.float32) for b in ("foley", "sfx", "music", "voice")}
    sources, notes = [], []
    fo = _foley()
    if fo is None:
        notes.append("foley.py missing: take audio silent")
    cut = int(float(edl.audio["cut_fade_ms"]) * sr / 1000.0)
    for s in edl.shots:
        s0, s1 = int(round(s.k0 / edl.fps * sr)), int(round(s.k1 / edl.fps * sr))
        # a freeze (speed 0) has no take sound of its own unless the EDL asks for it (it would run on under a still)
        if s.audio.get("take_audio", s.speed > 0) and s.src.kind == "take" and fo is not None:
            seg = _take_audio(fo, s, sr, s0, s1, cut, sources, notes)
            if seg is not None:
                _add(stems["foley"], s0, seg)
        for e in s.audio.get("extra") or []:
            path = resolve_file(e["wav"])
            x = decode_audio(path, sr)
            i0 = int(parse_time(e.get("in", 0.0)) * sr)
            x = x[i0:i0 + int(parse_time(e["dur"]) * sr)] if e.get("dur") is not None else x[i0:]
            x = x * ramp(x.shape[0], int(parse_time(e.get("fade_in", 0.002)) * sr),
                         int(parse_time(e.get("fade_out", 0.01)) * sr))[:, None]
            _add(stems[e.get("bus", "sfx")], s0 + int(round(parse_time(e.get("at", 0.0)) * sr)),
                 x * (10 ** (float(e.get("gain_db", 0.0)) / 20)))
            sources.append({"shot": s.id, "file": rel(path)})
    if edl.music and edl.music.get("wav"):
        stems["music"] = _music(edl, sr, n, sources)
    master = stems["foley"] + stems["sfx"] + stems["music"]
    if edl.voice and edl.voice.get("wav"):
        stems["voice"], env = _voice(edl, sr, n, sources)
        master = master * env[:, None] + stems["voice"]
    te = n - int(parse_time(edl.audio["tail_silence"]) * sr)          # the last 0.5 s are silent (TRAILER.md 4, #21)
    tf = min(te, int(parse_time(edl.audio["tail_fade"]) * sr))
    if 0 < te <= n:
        if tf > 0:
            master[te - tf:te] *= ramp(tf, 0, tf)[:, None]
        master[te:] = 0.0
    return stems, master, sources, notes


def _sliding_min(x, size):
    try:
        from scipy.ndimage import minimum_filter1d
        return minimum_filter1d(x, size, mode="nearest")
    except ImportError:                                   # numpy fallback, chunked to bound memory
        h = size // 2
        p = np.concatenate([np.full(h, x[0]), x, np.full(h, x[-1])])
        out = np.empty_like(x)
        from numpy.lib.stride_tricks import sliding_window_view
        for i in range(0, x.shape[0], 1 << 15):
            j = min(x.shape[0], i + (1 << 15))
            out[i:j] = sliding_window_view(p[i:j + 2 * h], size).min(axis=1)
        return out


def limit(x, thr, sr, look_ms=5.0, release_db_s=30.0):
    """Lookahead peak limiter (numpy): the gain reaches its floor exactly at each peak over thr (linear sample peak),
    starts falling look_ms before it and recovers at release_db_s. Only used when the -14 LUFS gain would clip."""
    peak = np.abs(x).max(axis=1).astype(np.float64)
    req = np.minimum(0.0, 20.0 * np.log10(thr / np.maximum(peak, 1e-12)))
    L = max(1, int(look_ms * sr / 1000.0))
    gm = _sliding_min(req, 2 * L + 1)
    r = release_db_s / float(sr)
    idx = np.arange(req.shape[0], dtype=np.float64)
    env = r * idx + np.minimum.accumulate(gm - r * idx)    # slew-limited recovery, never above the requirement
    M = 2 * (L // 2) + 1
    h = M // 2
    c = np.concatenate([[0.0], np.cumsum(np.concatenate([np.full(h, env[0]), env, np.full(h, env[-1])]))])
    env = (c[M:M + env.shape[0]] - c[:env.shape[0]]) / M
    return (x * (10.0 ** (env / 20.0)).astype(np.float32)[:, None]).astype(np.float32), float(-env.min())


def loudnorm_run(inp, af, out=None):
    """Runs one loudnorm pass; returns its JSON (floats where possible)."""
    cmd = [ffmpeg_exe(), "-hide_banner", "-nostats", "-i", str(inp), "-af", af]
    cmd += (["-ar", str(SR), "-c:a", "pcm_f32le", "-y", str(out)] if out else ["-f", "null", "-"])
    r = subprocess.run(cmd, capture_output=True, text=True, errors="replace")
    blocks = re.findall(r"\{[^{}]*\}", r.stderr, re.S)
    if r.returncode != 0 or not blocks:
        raise SystemExit("loudnorm failed: " + r.stderr.strip()[-600:])
    d = json.loads(blocks[-1])
    for key, v in list(d.items()):
        try:
            f = float(v)
            d[key] = f if math.isfinite(f) else str(v)     # "-inf" stays a string: the report must be strict JSON
        except (TypeError, ValueError):
            pass
    return d


def ebur128_measure(path):
    """(integrated LUFS, true peak dBTP) by ffmpeg's ebur128, the meter check_trailer.py uses."""
    r = subprocess.run([ffmpeg_exe(), "-hide_banner", "-nostats", "-i", str(path), "-af", "ebur128=peak=true", "-f", "null", "-"],
                       capture_output=True, text=True, errors="replace")
    tail = r.stderr[r.stderr.rfind("Summary:"):]
    mi = re.search(r"I:\s*(-?[\d.]+)\s*LUFS", tail)
    mt = re.search(r"True peak:.*?Peak:\s*(-?[\d.]+)", tail, re.S)
    return (float(mi.group(1)) if mi else None), (float(mt.group(1)) if mt else None)


def _measurable(m):
    return all(isinstance(m.get(q), float) for q in ("input_i", "input_tp", "input_lra", "input_thresh", "target_offset"))         and m["input_i"] >= -70.0


def master_loudness(x, sr, stem_dir, I=-14.0, TP=-1.0, max_gr=12.0, release_db_s=30.0):
    """Pre-master -> (limit if needed, at most max_gr dB) -> two-pass loudnorm linear=true -> stem_dir/mix.wav.
    Returns a report dict."""
    pre = stem_dir / "mix_pre.wav"
    write_wav_f32(pre, x, sr)
    out = stem_dir / "mix.wav"
    rep = {"target": {"I": I, "TP": TP}}
    base = "loudnorm=I=%g:TP=%g:LRA=11:print_format=json" % (I, TP)
    m = loudnorm_run(pre, base)
    rep["pass1"] = {k: m.get(k) for k in ("input_i", "input_tp", "input_lra", "input_thresh")}
    if not _measurable(m):
        write_wav_f32(out, x, sr)
        rep["result"] = "silent mix: not normalised"
        return out, rep
    rep["limiter"] = []
    x0, thr_db = x, None
    peak0_db = 20.0 * math.log10(max(1e-12, float(np.abs(x0).max())))
    ok = _measurable
    for it in range(8):
        g = I - m["input_i"]
        over_db = m["input_tp"] + g - (TP - 0.3)
        if over_db <= 0 or (thr_db is not None and peak0_db - thr_db >= max_gr - 1e-6):
            break
        # sample-peak ceiling before the gain; each round lowers it by what is still over (limiting also lowers I);
        # never more than max_gr below the loudest peak: a sparse mix (a few clicks) would chase I down forever
        thr_db = (TP - 0.6 - g) if thr_db is None else thr_db - max(0.1, over_db)
        thr_db = max(thr_db, peak0_db - max_gr)
        xl, gr = limit(x0, 10 ** (thr_db / 20.0), sr, release_db_s=release_db_s)
        write_wav_f32(pre, xl, sr)
        ml = loudnorm_run(pre, base)
        rep["limiter"].append({"ceiling_dbfs": round(thr_db, 2), "max_gr_db": round(gr, 2), "input_i": ml.get("input_i"),
                               "input_tp": ml.get("input_tp")})
        if not ok(ml):                     # unmeasurable after limiting: keep the previous round
            write_wav_f32(pre, x, sr)
            warn("loudnorm could not measure the limited mix (%s): limiter round %d dropped" % (ml.get("input_i"), it + 1))
            break
        x, m = xl, ml
    if rep["limiter"] and rep["limiter"][-1]["max_gr_db"] > 6.0:
        warn("the limiter takes up to %.1f dB off the loudest peaks: lower the transient sfx / foley gains"
             % rep["limiter"][-1]["max_gr_db"])
    # whatever the limiter could not fit comes off the loudness target: the true peak ceiling is the hard rule
    over_db = m["input_tp"] + (I - m["input_i"]) - (TP - 0.3)
    I_eff = I - max(0.0, over_db)
    if I_eff < I - 0.05:
        warn("mix normalised to %.1f LUFS instead of %.1f: its peaks need more than %.0f dB of limiting (sparse mix?)"
             % (I_eff, I, max_gr))
    rep["target"]["I_eff"] = round(I_eff, 2)
    lra = min(50.0, max(7.0, float(m["input_lra"]) + 1.0))    # linear mode keeps the LRA only if the target allows it
    af = ("loudnorm=I=%g:TP=%g:LRA=%g:measured_I=%g:measured_TP=%g:measured_LRA=%g:measured_thresh=%g:offset=%g:"
          "linear=true:print_format=json" % (I_eff, TP, lra, m["input_i"], m["input_tp"], m["input_lra"],
                                             m["input_thresh"], m["target_offset"]))
    m2 = loudnorm_run(pre, af, out)
    rep["pass2"] = {k: m2.get(k) for k in ("output_i", "output_tp", "output_lra", "normalization_type")}
    if m2.get("normalization_type") != "linear":
        warn("loudnorm fell back to %s normalisation (too little headroom after the limiter, or an LRA of 0)" % m2.get("normalization_type"))
    # loudnorm's own meter can differ from ebur128 by a few tenths on short or sparse mixes: trim the last bit linearly
    ei, etp = ebur128_measure(out)
    rep["ebur128"] = {"I": ei, "TP": etp}
    if ei is not None and etp is not None and abs(ei - I_eff) > 0.1:
        trim = I_eff - ei
        if etp + trim <= TP - 0.1:
            write_wav_f32(out, decode_audio(out, sr, cache=False) * (10 ** (trim / 20.0)), sr)
            ei, etp = ebur128_measure(out)
            rep["trim_db"], rep["ebur128"] = round(trim, 2), {"I": ei, "TP": etp}
        else:
            warn("mix is %.1f LUFS after loudnorm; a %.1f dB trim would put the true peak at %.1f dBTP: left as is"
                 % (ei, trim, etp + trim))
    return out, rep


def slice_audio(x, ranges, sr, fade_ms=5.0):
    """Concatenates [(t0, t1)] seconds of x (the --only review cut), with short fades at the joins."""
    parts = []
    f = int(fade_ms * sr / 1000.0)
    for t0, t1 in ranges:
        seg = x[int(round(t0 * sr)):int(round(t1 * sr))]
        if len(ranges) > 1:
            seg = seg * ramp(seg.shape[0], f, f)[:, None]
        parts.append(seg)
    return np.concatenate(parts) if parts else np.zeros((0, 2), np.float32)


# ------------------------------------------------------------------------------------------------ outputs
def tc(t):
    m, s = divmod(max(0.0, t), 60.0)
    return "%d:%04.1f" % (m, s)


def out_paths(out, stems_arg):
    out = Path(out)
    if not out.is_absolute() and str(out.parent) in ("", "."):   # a bare name: the master in the upload folder,
        out = (STORE / "trailer" if out.name == DEFAULT_OUT.name else WORK) / out     # review cuts in _work/trailer
    out = out.resolve()
    base = out.stem[:-5] if out.stem.endswith("_main") else out.stem
    default = out == DEFAULT_OUT.resolve()
    stems = Path(stems_arg).resolve() if stems_arg else (WORK / "stems" if default else out.parent / (base + "_stems"))
    return {"out": out, "cover": out.with_name(base + "_cover.png"), "sheet": out.with_name(base + "_sheet.jpg"),
            "stems": stems, "stills": (WORK if default else out.parent) / (base + "_stills"),
            "report": (WORK if default else out.parent) / (base + "_render.json")}


def contact_sheet(thumbs, path, cols=4):
    """thumbs = [(shot, label, uint8 frame)] -> one JPG, the timecode + caption under every frame."""
    if not thumbs:
        return
    tw, th, lab, gap = 480, 270, 40, 8
    rows = (len(thumbs) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * tw + (cols + 1) * gap, rows * (th + lab + gap) + gap), (22, 24, 29))
    d = ImageDraw.Draw(sheet)
    try:
        f = TT.ImageFont.truetype(str(TT.FONTS / "Rubik[wght].ttf"), 17)
    except OSError:
        f = None
    for i, (s, label, fr) in enumerate(thumbs):
        x = gap + (i % cols) * (tw + gap)
        y = gap + (i // cols) * (th + lab + gap)
        sheet.paste(Image.fromarray(fr).resize((tw, th), Image.Resampling.LANCZOS), (x, y))
        while f is not None and len(label) > 8 and d.textlength(label, font=f) > tw - 4:
            label = label[:-2] + "…"                 # drops one character per round, ends in an ellipsis
        d.text((x + 2, y + th + 8), label, fill=(232, 232, 236), font=f)
    sheet.save(path, quality=90, subsampling=0)


def shot_label(edl, s):
    cap = " / ".join(("%s %s" % (t.get("num"), t["text"]) if t.get("num") is not None else t["text"]) for t in s.titles)
    fx = ",".join(k for k in s.fx if k != "hold_last")
    return "#%s  %s-%s  %s%s%s" % (s.id, tc(s.k0 / edl.fps), tc(s.k1 / edl.fps), s.src.name,
                                   ("  [" + fx + "]") if fx else "", ("  " + cap) if cap else "")


def encoder_cmd(W, H, fps, out, audio_wav, crf, preset):
    cmd = [ffmpeg_exe(), "-hide_banner", "-nostats", "-v", "error", "-y",
           "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "%dx%d" % (W, H), "-framerate", "%g" % fps, "-i", "-"]
    if audio_wav:
        cmd += ["-i", str(audio_wav)]
    cmd += ["-map", "0:v:0"] + (["-map", "1:a:0"] if audio_wav else [])
    # make_clips.py's colour path: BT.709 matrix, tv range, and setparams stamps primaries / transfer on the frames
    cmd += ["-vf", "scale=out_color_matrix=bt709:out_range=tv:flags=lanczos+accurate_rnd+full_chroma_int,format=yuv420p,"
                   "setparams=range=tv:color_primaries=bt709:color_trc=bt709:colorspace=bt709",
            "-c:v", "libx264", "-preset", preset, "-profile:v", "high", "-pix_fmt", "yuv420p"]
    cmd += (["-crf", str(crf)] if crf else ["-b:v", "20M"]) + ["-maxrate", "24M", "-bufsize", "40M"]
    cmd += ["-g", str(int(round(2 * fps))), "-keyint_min", str(int(round(fps))), "-r", "%g" % fps, "-fps_mode", "cfr",
            "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709", "-color_range", "tv"]
    cmd += (["-c:a", "aac", "-profile:a", "aac_low", "-b:a", "192k", "-ar", str(SR), "-ac", "2"] if audio_wav else ["-an"])
    return cmd + ["-movflags", "+faststart", str(out)]


def select_frames(edl, only):
    """Timeline frame indices to render (all, or the --only shots in timeline order) and their time ranges."""
    if not only:
        return list(range(edl.n)), [(0.0, edl.n / edl.fps)], edl.shots
    want = [x.strip() for x in only.split(",") if x.strip()]
    shots = []
    for w in want:
        s = edl.by_id(w)
        if s is None:
            raise SystemExit("--only: no shot %r (have %s)" % (w, ", ".join(x.id for x in edl.shots)))
        shots.append(s)
    shots.sort(key=lambda s: s.k0)
    frames = [k for s in shots for k in range(s.k0, s.k1)]
    return frames, [(s.k0 / edl.fps, s.k1 / edl.fps) for s in shots], shots


# ------------------------------------------------------------------------------------------------ process pool
_W = {}


def _worker_init(edl_path, fps, W, H, date, cache):
    """Process-pool worker: its own EDL, caches and warnings (warnings come back with each chunk)."""
    _W.update(edl=Edl(edl_path, fps), W=W, H=H, date=date)
    DECODE.max, BACKGROUND.max = cache, cache


def _worker_chunk(ks):
    t0 = time.time()
    e = _W["edl"]
    n0 = len(WARNINGS)
    out = [render_frame(e, k, _W["W"], _W["H"], date=_W["date"]).tobytes() for k in ks]
    return out, time.time() - t0, WARNINGS[n0:]


# ------------------------------------------------------------------------------------------------ main
def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--edl", default=str(DEFAULT_EDL))
    ap.add_argument("--out", default=str(DEFAULT_OUT))
    ap.add_argument("--only", default="", help="comma-separated shot ids: render just these (a review cut)")
    ap.add_argument("--preview", action="store_true", help="half size (960x540 for the trailer), veryfast, no cover")
    ap.add_argument("--frames-at", default="", help="comma-separated trailer seconds: dump PNG stills and exit")
    ap.add_argument("--no-audio", action="store_true", help="no audio track at all")
    ap.add_argument("--jobs", type=int, default=min(16, os.cpu_count() or 4))
    ap.add_argument("--date", default=None, help="override the end card's date layer (e.g. 'Out now')")
    ap.add_argument("--crf", type=int, default=0, help="CRF with the 24M cap instead of the 20 Mbit/s ABR")
    ap.add_argument("--preset", default="")
    ap.add_argument("--stems", default="", help="stem folder (default: see the module doc)")
    ap.add_argument("--procs", type=int, default=0, help="render on N processes (chunks of --chunk frames) instead of threads")
    ap.add_argument("--chunk", type=int, default=6)
    ap.add_argument("--audio-only", action="store_true",
                    help="rebuild only the mix and remux it over the existing --out video (stream copy, no frame render)")
    a = ap.parse_args(argv)
    t_start = time.time()
    if not ffmpeg_exe():
        raise SystemExit("ffmpeg not available: pip install imageio-ffmpeg")
    edl = Edl(a.edl)
    W, H = (edl.size[0] // 2, edl.size[1] // 2) if a.preview else edl.size
    jobs = max(1, a.jobs)
    DECODE.max = max(8, jobs + 4)
    BACKGROUND.max = max(8, 2 * jobs + 2)
    P = out_paths(a.out, a.stems)

    if a.frames_at:                                        # review stills: no encode
        P["stills"].mkdir(parents=True, exist_ok=True)
        done = []
        for t in [parse_time(x) for x in a.frames_at.split(",") if x.strip()]:
            k = min(max(0, int(round(t * edl.fps))), edl.n - 1)
            t0 = time.time()
            fr = render_frame(edl, k, W, H, date=a.date)
            q = P["stills"] / ("t%06.2f.png" % (k / edl.fps))
            Image.fromarray(fr).save(q, compress_level=3)
            s = edl.shot_at(k)
            done.append({"t": round(k / edl.fps, 3), "shot": s.id if s else None, "png": rel(q),
                         "ms": round((time.time() - t0) * 1000)})
        print(json.dumps({"stills": done, "warnings": WARNINGS}))
        return 0

    frames, ranges, shots = select_frames(edl, a.only)
    seconds = len(frames) / edl.fps
    P["out"].parent.mkdir(parents=True, exist_ok=True)
    audio_wav, audio_rep, sources, notes = None, None, [], []
    if not a.no_audio:
        ta = time.time()
        P["stems"].mkdir(parents=True, exist_ok=True)
        stems, master, sources, notes = build_mix(edl, SR)
        for name, x in stems.items():
            write_wav_f32(P["stems"] / (name + ".wav"), x, SR)
        if a.only:
            master = slice_audio(master, ranges, SR)
        audio_wav, audio_rep = master_loudness(master, SR, P["stems"], float(edl.audio["lufs"]), float(edl.audio["tp"]),
                                              float(edl.audio.get("max_limit_db", 12.0)),
                                              float(edl.audio.get("limiter_release_db_s", 30.0)))
        audio_rep["sec"] = round(time.time() - ta, 1)

    if a.audio_only:                                       # audio iteration: keep the rendered picture, swap the sound
        if audio_wav is None or not P["out"].exists():
            raise SystemExit("--audio-only needs an existing --out video and audio enabled")
        tmp = P["out"].with_name(P["out"].stem + "_remux" + P["out"].suffix)
        cmd = [ffmpeg_exe(), "-v", "error", "-y", "-i", str(P["out"]), "-i", str(audio_wav), "-map", "0:v:0", "-map", "1:a:0",
               "-c:v", "copy", "-c:a", "aac", "-profile:a", "aac_low", "-b:a", "192k", "-ar", str(SR), "-ac", "2",
               "-movflags", "+faststart", "-shortest", str(tmp)]
        r = subprocess.run(cmd, capture_output=True)
        if r.returncode != 0:
            raise SystemExit("ffmpeg remux failed: " + r.stderr.decode("utf-8", "replace").strip())
        os.replace(tmp, P["out"])
        info = probe_media(P["out"])
        rep = json.loads(P["report"].read_text(encoding="utf-8")) if P["report"].exists() else {}
        rep.update({"audio_master": audio_rep, "audio_notes": notes, "audio_sources": sources, "audio": info["audio"],
                    "mb": round(P["out"].stat().st_size / 1e6, 2), "audio_only_remux": True, "warnings": WARNINGS})
        P["report"].parent.mkdir(parents=True, exist_ok=True)
        P["report"].write_text(json.dumps(rep, indent=1), encoding="utf-8")
        print(json.dumps({"out": rel(P["out"]), "remux": "audio only", "audio_master": audio_rep, "warnings": WARNINGS}))
        return 0

    preset = a.preset or ("veryfast" if a.preview else "slow")
    crf = a.crf or (20 if a.preview else 0)
    proc = subprocess.Popen(encoder_cmd(W, H, edl.fps, P["out"], audio_wav, crf, preset),
                            stdin=subprocess.PIPE, stderr=subprocess.PIPE)
    err_buf = []
    threading.Thread(target=lambda: err_buf.append(proc.stderr.read()), daemon=True).start()
    mid = {(s.k0 + s.k1) // 2: s for s in shots}
    thumbs, busy = [], []
    busy_lock = threading.Lock()

    def job(k):
        t0 = time.time()
        fr = render_frame(edl, k, W, H, date=a.date)
        with busy_lock:
            busy.append(time.time() - t0)
        return fr

    t_render, last_print = time.time(), [time.time()]

    def emit(p, k, fr):
        """Writes output frame p (timeline frame k); False when ffmpeg went away."""
        try:
            proc.stdin.write(fr if isinstance(fr, bytes) else fr.tobytes())
        except (BrokenPipeError, OSError):
            return False
        if k in mid:
            arr = np.frombuffer(fr, np.uint8).reshape(H, W, 3) if isinstance(fr, bytes) else fr
            thumbs.append((mid[k], shot_label(edl, mid[k]), arr))
        if time.time() - last_print[0] > 10:
            last_print[0] = time.time()
            el = last_print[0] - t_render
            print("  frame %d/%d  %.1f fps  eta %.0f s" % (p + 1, len(frames), (p + 1) / el,
                                                            (len(frames) - p - 1) * el / (p + 1)), file=sys.stderr)
        return True

    if a.procs > 0:
        from concurrent.futures import ProcessPoolExecutor
        chunks = [frames[i:i + max(1, a.chunk)] for i in range(0, len(frames), max(1, a.chunk))]
        jobs = a.procs
        with ProcessPoolExecutor(max_workers=a.procs, initializer=_worker_init,
                                 initargs=(str(edl.path), edl.fps, W, H, a.date, max(6, a.chunk + 2))) as pool:
            futs, q, p, window = {}, 0, 0, 2 * a.procs
            for ci in range(len(chunks)):
                while q < len(chunks) and q - ci < window:
                    futs[q] = pool.submit(_worker_chunk, chunks[q])
                    q += 1
                out, sec, wr = futs.pop(ci).result()
                for m_ in wr:
                    warn(m_)
                busy.extend([sec / len(out)] * len(out))
                ok = True
                for fr, k in zip(out, chunks[ci]):
                    ok = emit(p, k, fr)
                    p += 1
                    if not ok:
                        break
                if not ok:
                    break
    else:
        futures, q, window = {}, 0, 2 * jobs
        with ThreadPoolExecutor(max_workers=jobs) as pool:
            for p, k in enumerate(frames):
                while q < len(frames) and q - p < window:
                    futures[q] = pool.submit(job, frames[q])
                    q += 1
                if not emit(p, k, futures.pop(p).result()):
                    break
    proc.stdin.close()
    rc = proc.wait()
    if rc != 0:
        time.sleep(0.2)
        raise SystemExit("ffmpeg failed: " + (err_buf[0] if err_buf else b"").decode("utf-8", "replace").strip())
    render_s = time.time() - t_render
    contact_sheet(thumbs, P["sheet"])
    cover = None
    if not a.preview and edl.shot_at(int(round(edl.cover_t * edl.fps))) is not None:
        fr = render_frame(edl, int(round(edl.cover_t * edl.fps)), edl.size[0], edl.size[1], overlays=False)
        Image.fromarray(fr).save(P["cover"], optimize=True)
        cover = rel(P["cover"])
    info = probe_media(P["out"])
    result = {
        "out": rel(P["out"]), "seconds": round(info["duration"], 3) if info["duration"] else None, "fps": info["fps"],
        "frames": info["frames"], "size": [info["width"], info["height"]], "codec": info["codec"],
        "pix_fmt": info["pix_fmt"], "color": info["color"], "audio": info["audio"], "mb": round(P["out"].stat().st_size / 1e6, 2),
        "mbps": round(P["out"].stat().st_size * 8 / 1e6 / max(1e-6, seconds), 2),
        "planned": {"frames": len(frames), "seconds": round(seconds, 3), "shots": [s.id for s in shots],
                    "size": [W, H], "preset": preset, "crf": crf or None},
        "timing": {"render_s": round(render_s, 1), "out_fps": round(len(frames) / max(1e-6, render_s), 2),
                   "ms_per_frame_wall": round(render_s * 1000 / max(1, len(frames)), 1),
                   "ms_per_frame_thread": round(1000 * sum(busy) / max(1, len(busy)), 1), "jobs": jobs,
                   "decode_cache": [DECODE.hits, DECODE.misses], "bg_cache": [BACKGROUND.hits, BACKGROUND.misses]},
        "audio_master": audio_rep, "audio_notes": notes, "cover": cover, "sheet": rel(P["sheet"]),
        "stems": rel(P["stems"]) if not a.no_audio else None, "warnings": WARNINGS, "sec": round(time.time() - t_start, 1),
    }
    P["report"].parent.mkdir(parents=True, exist_ok=True)
    P["report"].write_text(json.dumps(dict(result, edl=rel(edl.path), audio_sources=sources), indent=1), encoding="utf-8")
    result["report"] = rel(P["report"])
    print(json.dumps(result))
    return 0


if __name__ == "__main__":
    sys.exit(main())
