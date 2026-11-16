"""Prints every caption / end-card box of the TikTok EDL as fractions of the 1080x1920 frame, and flags boxes that
leave TikTok's UI-safe area (Docs/TIKTOK.md 4: x 0.06-0.84 next to the right-hand buttons, y 0.10-0.78)."""
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "trailer"))
import titles as T  # noqa: E402

W, H = 1080, 1920
SAFE = (0.06, 0.10, 0.84, 0.78)


def check(name, box):
    f = (box[0] / W, box[1] / H, box[2] / W, box[3] / H)
    ok = f[0] >= SAFE[0] and f[1] >= SAFE[1] and f[2] <= SAFE[2] and f[3] <= SAFE[3]
    print("%-28s x %.3f-%.3f  y %.3f-%.3f  %s" % (name, f[0], f[2], f[1], f[3], "ok" if ok else "OUTSIDE"))
    return ok


def main():
    edl = json.loads((HERE / "edl_tiktok.json").read_text(encoding="utf-8"))
    good = True
    for s in edl["shots"]:
        ts = s.get("titles") or ([s["title"]] if s.get("title") else [])
        for t in ts:
            good &= check(t["text"], T.title_box(t, W, H))
        for f in s.get("fx") or []:
            if isinstance(f, dict) and f.get("type") == "endcard":
                cfg = {a: b for a, b in f.items() if a != "type"}
                for k, b in T.endcard_boxes(cfg, W, H).items():
                    if k == "logo" or cfg.get(k):
                        good &= check("endcard " + k, b)
    print("ALL OK" if good else "SOME OUTSIDE")
    return 0 if good else 1


if __name__ == "__main__":
    sys.exit(main())
