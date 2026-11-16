"""Check frame continuity, centred safe captions and the silent VO scrape gap."""
import json
import hashlib
import sys
import wave
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
sys.path.insert(0, str(ROOT / "Tools/steam_art/trailer"))
import titles as T  # noqa: E402

OUT = ROOT / "SteamStore/tiktok/scraper_upgrades/human_cut"


def check_box(name, b):
    f = [b[0] / 1080, b[1] / 1920, b[2] / 1080, b[3] / 1920]
    assert f[0] >= .06 and f[1] >= .10 and f[2] <= .84 and f[3] <= .78, (name, f)
    assert abs((f[0] + f[2]) / 2 - .5) < .001, (name, f)
    return {"text": name, "center_x": (f[0] + f[2]) / 2, "safe": True}


def main():
    edl = json.loads((HERE / "edl.json").read_text(encoding="utf-8"))
    report = {"titles": [], "cards": []}
    last = 0
    for s in edl["shots"]:
        k, n = round(s["start"] * 60), round(s["dur"] * 60)
        assert k == last, (k, last)
        assert s["speed"] == 1.0
        last = k + n
        for t in s.get("titles", []):
            assert t["pos"]["x"] == .5
            assert t["at"] + t["dur"] <= s["dur"] + 1e-6
            report["titles"].append(check_box(t["text"], T.title_box(t, 1080, 1920)))
        for fx in s.get("fx", []):
            if isinstance(fx, dict) and fx.get("type") == "endcard":
                assert fx["cx"] == .5
                cfg = {a: b for a, b in fx.items() if a != "type"}
                for key, b in T.endcard_boxes(cfg, 1080, 1920).items():
                    if key == "logo" or cfg.get(key):
                        report["cards"].append(check_box(key, b))
    assert last == 2022
    with wave.open(str(OUT / "vo_aligned.wav")) as w:
        assert w.getframerate() == 48000 and w.getnchannels() == 2
        x = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").reshape(-1, 2)
    assert len(x) == round(33.7 * 48000)
    assert np.count_nonzero(x[round(18.38 * 48000):round(26.64 * 48000)]) == 0
    timing = json.loads((OUT / "timing.json").read_text(encoding="utf-8"))
    report["human_recordings"] = {}
    for kind, meta in timing["human_recordings"].items():
        raw = (ROOT / meta["file"]).read_bytes()
        take = json.loads(raw)
        assert hashlib.sha256(raw).hexdigest() == meta["sha256"], kind
        assert take["fixture"] is False and take["kind"] == kind
        assert len(take["samples"]) == meta["samples"]
        assert abs(take["duration"] - meta["duration"]) < 1e-9
        assert all(b["t"] >= a["t"] for a, b in zip(take["samples"], take["samples"][1:]))
        report["human_recordings"][kind] = {"samples": meta["samples"], "seconds": meta["duration"],
                                               "sha256_unchanged": True}
    report.update(frames=last, seconds=last / 60, voice_frames=len(x),
                  silent_vo=[18.38, 26.64], passed=True)
    (OUT / "alignment_check.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
