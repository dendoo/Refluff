"""ffmpeg helpers shared by make_clips.py and verify_assets.py (Docs/STORE_ART.md, description clips).

ffprobe is not bundled with imageio-ffmpeg, so a clip is inspected by parsing `ffmpeg -i` (container, streams) and by
a stream-copy pass to a null muxer (the exact frame count).

    python Tools/steam_art/clip_probe.py CLIP [CLIP ...]     -> one JSON line per clip
"""
import json
import re
import subprocess
import sys


def ffmpeg_exe():
    """The imageio-ffmpeg binary (ffmpeg 7.1 with libx264), or None when the package is missing."""
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        return None


def _split_top(s):
    """Splits an ffmpeg stream description on commas that are not inside parentheses or brackets."""
    out, depth, cur = [], 0, []
    for ch in s:
        if ch in "([":
            depth += 1
        elif ch in ")]":
            depth -= 1
        if ch == "," and depth == 0:
            out.append("".join(cur).strip())
            cur = []
        else:
            cur.append(ch)
    if cur:
        out.append("".join(cur).strip())
    return out


def probe_media(path, count_frames=True):
    """{"duration", "width", "height", "codec", "pix_fmt", "color", "fps", "audio", "streams", "frames"} for a video or GIF.
    Raises RuntimeError when ffmpeg is missing or cannot read the file."""
    ff = ffmpeg_exe()
    if not ff:
        raise RuntimeError("ffmpeg not available (pip install imageio-ffmpeg)")
    r = subprocess.run([ff, "-hide_banner", "-i", str(path)], capture_output=True, text=True, errors="replace")
    txt = r.stderr
    info = {"duration": None, "width": None, "height": None, "codec": None, "pix_fmt": None, "color": None,
            "fps": None, "audio": False, "streams": [], "frames": None}
    m = re.search(r"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)", txt)
    if m:
        info["duration"] = int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))
    for line in txt.splitlines():
        sm = re.match(r"\s*Stream #\d+:\d+\S*: (\w+): (.*)$", line)
        if not sm:
            continue
        kind, desc = sm.group(1), sm.group(2)
        info["streams"].append(kind.lower())
        if kind == "Audio":
            info["audio"] = True
        if kind != "Video" or info["codec"] is not None:
            continue
        parts = _split_top(desc)
        info["codec"] = parts[0].split()[0] if parts else None
        if len(parts) > 1:
            pm = re.match(r"(\w+)(?:\((.*)\))?", parts[1])
            if pm:
                info["pix_fmt"] = pm.group(1)
                info["color"] = pm.group(2)
        for p in parts:
            wm = re.match(r"(\d+)x(\d+)", p)
            if wm and info["width"] is None:
                info["width"], info["height"] = int(wm.group(1)), int(wm.group(2))
            fm = re.match(r"([\d.]+)\s+fps", p)
            if fm:
                info["fps"] = float(fm.group(1))
    if info["width"] is None and info["duration"] is None:
        raise RuntimeError("ffmpeg cannot read %s: %s" % (path, txt.strip().splitlines()[-1] if txt.strip() else "no output"))
    if count_frames and "video" in info["streams"]:
        r2 = subprocess.run([ff, "-hide_banner", "-i", str(path), "-map", "0:v:0", "-c", "copy", "-f", "null", "-"],
                            capture_output=True, text=True, errors="replace")
        fr = re.findall(r"frame=\s*(\d+)", r2.stderr)
        if fr:
            info["frames"] = int(fr[-1])
    return info


if __name__ == "__main__":
    for a in sys.argv[1:]:
        print(json.dumps(dict(probe_media(a), path=a)))
