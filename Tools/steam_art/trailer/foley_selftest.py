"""Self-test for foley.py without Unity: builds a synthetic take whose audio.jsonl states come from a Python port of
ToolAudio.StepFrame (eases, LoopLayer volumes, machineOn, F3-rounded like DebugStateJson), renders it and measures it.

    python Tools/steam_art/trailer/foley_selftest.py OUT_DIR

Scenario (17.2 s at 60 Steps/s): dry vac press 0.3 s still -> 2 s drag with speed ramps -> release, 3 scheduled
rattle events; Hose burst 1.2 s; Brush (loop-layer machine, power one-shots) 1.5 s; dry vac again, re-pressed while it
coasts and held into its loop; a foam burst long enough to stitch into its loop; then UI one-shots (one through
audio_clips.json {"wav": ...}) and a looped generic play / stop.
Writes OUT_DIR/take (the synthetic take), foley_full.wav + stems/, foley_window.wav and prints a JSON report.
"""
import json
import math
import os
import shutil
import subprocess
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import foley as F  # noqa: E402

DT = 1.0 / 60.0


class FakeToolAudio:
    """ToolAudio.StepFrame + LoopLayer.Tick for the fields DebugStateJson prints (not the audio)."""

    def __init__(self):
        self.tool = None
        self.spin = self.speed01 = 0.0
        self.since = 999.0
        self.on = self.ever = False
        self.layers = {k: {"clip": "", "vol": 0.0, "pitch": 1.0, "cut": F.OPEN_HZ} for k in ("motor", "contact")}
        self.rattle = self.dropped = 0
        self.last_lead = 0.0
        self.recs = {}

    @staticmethod
    def uses_recording(tool):
        e = F.entry(tool)
        return bool(e.get("powered")) and bool(e.get("useRecording")) and F.machine_recording(tool) is not None

    @staticmethod
    def slot(tool, slot, recipe_key):
        e = F.entry(tool)
        name = F.clip_name(F.clip_for_guid(e.get(slot)))
        r = int(e.get(recipe_key, 0) or 0)
        return name or ("sfx_%s_%s" % (tool, F.RECIPES[r]) if r else "")

    def _apply_clips(self):
        rec = self.uses_recording(self.tool)
        want = {"motor": "" if rec else self.slot(self.tool, "motorLoop", "motorRecipe"),
                "contact": self.slot(self.tool, "contactLoop", "contactRecipe")}
        for k, L in self.layers.items():
            if L["clip"] != want[k]:
                L["clip"], L["vol"] = want[k], 0.0          # the new clip starts silent (the old one crossfades)

    def step(self, tool, stroking, speed_mm):
        if tool != self.tool:
            self.on, self.spin, self.ever = False, 0.0, False
            self.tool = tool
            self._apply_clips()
        e = F.entry(tool)
        self.since = 0.0 if stroking else self.since + DT
        active = self.since <= 0.12
        rec = self.uses_recording(tool)
        powered = bool(e.get("powered"))
        if rec:
            self.on = active
            if tool not in self.recs:
                self.recs[tool] = F.machine_recording(tool)
        elif powered and active and not self.on:
            self.on = self.ever = True
        elif self.on and not active:
            self.on = False
        tgt = 1.0 if self.on else 0.0
        rate = max(0.1, float(e.get("spinUpHz", 6)) if self.on else float(e.get("spinDownHz", 4.5)))
        self.spin += (tgt - self.spin) * (1 - math.exp(-rate * DT))
        self.spin = 0.0 if self.spin < 0.0015 else self.spin
        ref = max(1.0, float(e.get("contactRefSpeedMm", 600)))
        raw = min(1.0, speed_mm / ref) if active else 0.0
        self.speed01 += (raw - self.speed01) * (1 - math.exp(-9.0 * DT))
        self.speed01 = 0.0 if self.speed01 < 0.001 else self.speed01
        master = 0.85
        k = 1 - math.exp(-14.0 * DT)
        mv = master * float(e.get("motorVolume", 0.5)) * self.spin
        mp = float(e.get("motorPitch", 1)) * (float(e.get("spinPitchMin", .6)) + (1 - float(e.get("spinPitchMin", .6))) * self.spin) \
            * (1 + float(e.get("motorSpeedPitch", 0)) * self.speed01)
        mc = self._loglerp(float(e.get("motorCutoffIdleHz", 1200)), F.OPEN_HZ, self.spin)
        drive = (float(e.get("contactIdle", .1)) + (1 - float(e.get("contactIdle", .1))) * self.speed01) * (1.0 if active else 0.0)
        cv = master * float(e.get("contactVolume", .55)) * drive
        cp = 1 + float(e.get("contactSpeedPitch", 0)) * (self.speed01 - 0.5)
        cc = min(F.OPEN_HZ, self._loglerp(float(e.get("contactCutoffSlowHz", 2200)), F.OPEN_HZ, self.speed01 ** 0.8))
        for key, (tv, tp, tc) in (("motor", (mv, mp, mc)), ("contact", (cv, cp, cc))):
            L = self.layers[key]
            if not L["clip"]:
                continue
            v = L["vol"] + (tv - L["vol"]) * k
            v = 0.0 if (v < 0.0008 and tv <= 0) else v
            L["vol"], L["pitch"], L["cut"] = v, min(3.0, max(0.1, tp)), min(F.OPEN_HZ, max(20.0, tc))
        return self.state()

    @staticmethod
    def _loglerp(a, b, t):
        a, b = min(max(a, 20), F.OPEN_HZ), min(max(b, 20), F.OPEN_HZ)
        return a * (b / a) ** min(1.0, max(0.0, t))

    def state(self):
        r3 = lambda x: round(float(x), 3)                                 # DebugStateJson prints F3
        m, c = self.layers["motor"], self.layers["contact"]
        rec = {}
        for tool, mr in self.recs.items():
            # what an unfocused Editor logs: dspTime frozen, so the phase never gets past Starting
            voices = [] if tool == "Hose" else [
                {"role": "start", "clip": mr.start, "startsInMs": 42.0, "pos": 0.0, "gain": 1.0, "volume": 0.357,
                 "pitch": 1.0, "timeSamples": 0, "isPlaying": False},
                {"role": "loop", "clip": mr.loop, "startsInMs": 42.0 + mr.start_len * 1000, "pos": 0.0, "gain": 1.0,
                 "volume": 0.357, "pitch": 1.0, "timeSamples": 0, "isPlaying": False}]
            rec[tool] = {"phase": "Starting", "speed": 0.0, "dsp": 21897.856, "leadMs": 42.0, "voices": voices}
        return {"tool": self.tool, "muted": False, "spin": r3(self.spin), "speed01": r3(self.speed01),
                "machineOn": self.on, "motorClip": m["clip"], "motorVol": r3(m["vol"]), "motorPitch": r3(m["pitch"]),
                "motorCut": r3(m["cut"]), "contactClip": c["clip"], "contactVol": r3(c["vol"]),
                "contactPitch": r3(c["pitch"]), "contactCut": r3(c["cut"]), "powered": bool(F.entry(self.tool).get("powered")),
                "pending": False, "rattleScheduled": self.rattle, "rattleLate": 0, "rattleDropped": self.dropped, "rattleStolen": 0,
                "lastArrivalMs": 150.0 if self.rattle else 0.0, "lastLeadMs": r3(self.last_lead * 1000), "generated": 0,
                "recorded": self.uses_recording(self.tool), "rec": rec}


def scenario(t):
    """(tool, stroking, stroke speed mm/s) at take time t."""
    if t < 3.6:
        if 0.5 <= t < 0.8:
            return "DryVacuum", True, 0.0
        if 0.8 <= t < 2.8:
            u = t - 0.8
            sp = 800.0 * min(1.0, u / 0.6) if u < 1.4 else 800.0 - 600.0 * (u - 1.4) / 0.6
            return "DryVacuum", True, sp
        return "DryVacuum", False, 0.0
    if t < 5.8:
        return "Hose", 4.0 <= t < 5.2, 300.0
    if t < 8.2:
        return "Brush", 6.2 <= t < 7.7, 400.0 + 200.0 * math.sin(6.0 * (t - 6.2))
    if t < 13.4:
        return "DryVacuum", (8.5 <= t < 9.3) or (9.7 <= t < 12.8), 500.0     # re-press while coasting, into the loop
    return "FoamSprayer", 13.8 <= t < 15.0, 250.0


def build_take(take_dir, seconds=17.2):
    if os.path.isdir(take_dir):
        shutil.rmtree(take_dir)
    os.makedirs(os.path.join(take_dir, "_clips"))
    shutil.copy(F.resolve_clip("ui_sparkle"), os.path.join(take_dir, "_clips", "ui_sparkle_rt.wav"))
    with open(os.path.join(take_dir, "audio_clips.json"), "w", encoding="utf-8") as fh:
        json.dump({"ui_sparkle_rt": {"wav": "_clips/ui_sparkle_rt.wav"},
                   "ui_kaching": {"asset": "Assets/CarpetCleaning/Audio/Generated/ui_kaching.wav"}}, fh, indent=1)
    rattle_at = {72: 2, 96: 1, 126: 3}                    # Step -> debris variant (1.2 s, 1.6 s, 2.1 s)
    ui = {924: ("ui_tick", 0.238, 1.0), 930: ("ui_tick", 0.238, 1.06), 936: ("ui_tick", 0.238, 1.12),
          960: ("ui_kaching", 0.578, 1.0), 978: ("ui_sparkle_rt", 0.306, 1.0)}
    fake = FakeToolAudio()
    lines = [json.dumps({"type": "header", "take": "foley_selftest", "stepDt": 0.016667, "fps": 60, "sampleRate": 48000})]
    for s in range(1, int(round(seconds / DT)) + 1):
        t = s * DT
        tool, stroking, sp = scenario(t)
        ev = []
        if s in (150, 151):                                # two strikes the frozen clock dropped (2.5 s)
            fake.dropped += 1
        if s in rattle_at:
            fake.rattle += 1
            fake.last_lead = 0.18
            ev.append({"src": "ToolAudio", "kind": "scheduled", "clip": "tool_dryvacuum_debris_%d" % rattle_at[s],
                       "vol": 0.41, "pitch": 1.03, "delay": 0.18, "pan": 0.0, "loop": False, "id": 1000 + rattle_at[s]})
        if s in ui:
            n, v, p = ui[s]
            ev.append({"src": "UiAudio", "kind": "oneshot", "clip": n, "vol": v, "pitch": p, "delay": 0.0, "pan": 0.0,
                       "loop": False, "id": 2000 + s % 12})
        if s == 984:
            ev.append({"src": "Handover", "kind": "play", "clip": "hose_pressurewasher_loop", "vol": 0.2, "pitch": 1.0,
                       "delay": 0.0, "pan": -0.3, "loop": True, "id": 77})
        if s == 1014:
            ev.append({"src": "Handover", "kind": "stop", "clip": "hose_pressurewasher_loop", "vol": 0.0, "pitch": 1.0,
                       "delay": 0.0, "pan": 0.0, "loop": True, "id": 77})
        st = fake.step(tool, stroking, sp)
        lines.append(json.dumps({"type": "step", "s": s, "t": round(t, 6), "state": st, "ev": ev}))
    with open(os.path.join(take_dir, "audio.jsonl"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines) + "\n")


# ------------------------------------------------------------------------------------------------ measurements
def seams(plan):
    """(time, kind, layer) of every join the renderer makes: stitches, crossfades, loop wraps, starts and stops."""
    out = []
    for v in plan.voices:
        role = v.info.get("role", v.kind)
        # a clip played from its top out of silence: its own attack, measured against the source by onsets()
        out.append((v.t_start, ("onset:" if not v.loop and v.offset == 0 and v.fade_in == 0 else "start:") + role, v.layer))
        if math.isfinite(v.t_end):
            out.append((v.t_end, ("stitch" if v.end_fade == 0 and role == "start" else "end:") + ("" if role == "start" and v.end_fade == 0 else role), v.layer))
        if math.isfinite(v.fade_out_at):
            out.append((v.fade_out_at, "xfade:" + role, v.layer))
        if v.loop:
            rate_src, _src, n_clip = F._source(v, plan.sr)
            length = n_clip / float(rate_src)
            rate = float(v.rate_v[0]) if v.rate_v else v.rate
            k, end = 1, min(v.t_end, plan.take.duration)
            while True:
                tw = v.t_start + (k * length - v.offset) / rate
                if tw >= end:
                    break
                if tw > v.t_start:
                    out.append((tw, "wrap:" + role, v.layer))
                k += 1
    return sorted(out)


def jump_ratio(x, sr, t, half_ms=2.0, around_ms=100.0):
    """Max sample-to-sample jump within +-half_ms of t over the 95th percentile of the same statistic in +-around_ms
    (a click stands out of its own neighbourhood), plus that jump over the local RMS (+-10 ms)."""
    n = int(round(t * sr))
    h, a = int(sr * half_ms / 1000), int(sr * around_ms / 1000)
    lo, hi = max(1, n - a), min(len(x) - 1, n + a)
    if hi - lo < 4 * h:
        return None
    d = np.max(np.abs(np.diff(x[lo - 1:hi + 1], axis=0)), axis=1)
    c = n - lo
    seam = float(np.max(d[max(0, c - h):c + h + 1]))
    wins = [float(np.max(d[i:i + 2 * h + 1])) for i in range(0, len(d) - 2 * h, h) if abs(i + h - c) > 2 * h]
    base = float(np.percentile(wins, 95)) if wins else 0.0
    r = int(sr * 0.01)
    rms = float(np.sqrt(np.mean(x[max(0, n - r):n + r].astype(np.float64) ** 2)))
    return {"ratio": seam / (base + 1e-9), "overRms": seam / (rms + 1e-9), "jump": seam, "level": rms}


def ebur128(path):
    sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    from clip_probe import ffmpeg_exe
    p = subprocess.run([ffmpeg_exe(), "-hide_banner", "-nostats", "-i", path, "-af", "ebur128=peak=true", "-f", "null", "-"],
                       capture_output=True, text=True)
    txt = p.stderr[p.stderr.rfind("Summary:"):]
    get = lambda key: float(txt.split(key, 1)[1].split()[0]) if key in txt else None
    return {"integratedLufs": get("I:"), "lra": get("LRA:"), "truePeakDbtp": get("Peak:")}


def stitch_exact(plan, stems, sr):
    """The spin-up -> loop join must be the two clips back to back, sample for sample (x the machine's volume)."""
    out = []
    for v in plan.voices:
        if v.kind == "machine" and v.info.get("role") == "start" and v.end_fade == 0 and math.isfinite(v.t_end):
            n, h = int(round(v.t_end * sr)), 96          # +-2 ms: the loop's load-pitch ramp starts after the next Step
            y = stems[v.layer][n - h:n + h]
            start = F.load_wav(v.info["path"], sr)
            loop = F.load_wav(v.info["tail_path"], sr)
            vol = float(v.gain_v[-1])
            ref = vol * np.concatenate([start[-h:], loop[:h]])
            out.append({"t": round(v.t_end, 4), "layer": v.layer, "maxAbsDiff": float(np.max(np.abs(y - ref)))})
    return out


def onsets(plan, stems, sr):
    """Worst rendered attack (max jump in the first 3 ms, from 0.2 ms before the start) over the source clip's own
    attack played out of silence x the voice's gain: 1 = the renderer adds nothing to the clip's own onset."""
    worst = None
    for v in plan.voices:
        if v.loop or v.offset != 0 or v.fade_in != 0:
            continue
        ns, h = int(round(v.t_start * sr)), int(0.003 * sr)
        src = np.concatenate([np.zeros((8, 2), np.float32), F.load_wav(v.info["path"], sr)[:h]])
        js = float(np.max(np.abs(np.diff(src, axis=0)))) * float(v.gain_v[0] if v.gain_v else v.gain)
        y = stems[v.layer][ns - 10:ns + h]
        jr = float(np.max(np.abs(np.diff(y, axis=0))))
        r = {"t": round(v.t_start, 4), "clip": v.clip, "ratio": round(jr / (js + 1e-9), 3), "layer": v.layer}
        if js > 1e-5 and (worst is None or r["ratio"] > worst["ratio"]):
            worst = r
    return worst


def spike_scan(x, sr, top=4, floor_db=-66.0):
    """Second-difference spikes over their own 10 ms RMS (Gaussian noise tops out near 5-6 over a few million
    samples); the largest few with their times. Only where the signal is above floor_db."""
    d2 = np.max(np.abs(x[2:] - 2 * x[1:-1] + x[:-2]), axis=1).astype(np.float64)
    w = int(sr * 0.01)
    c = np.concatenate([[0.0], np.cumsum(d2 ** 2)])
    idx = np.arange(len(d2))
    lo, hi = np.maximum(0, idx - w), np.minimum(len(d2), idx + w)
    rms2 = np.sqrt((c[hi] - c[lo]) / (hi - lo))
    lvl = np.sqrt(np.convolve(np.mean(x[1:-1].astype(np.float64) ** 2, axis=1), np.ones(w) / w, mode="same"))
    ratio = np.where(lvl > 10 ** (floor_db / 20), d2 / (rms2 + 1e-12), 0.0)
    out = []
    for _ in range(top):
        i = int(np.argmax(ratio))
        if ratio[i] <= 0:
            break
        out.append({"t": round((i + 1) / sr, 4), "ratio": round(float(ratio[i]), 2)})
        ratio[max(0, i - sr // 20):i + sr // 20] = 0.0
    return out


# ------------------------------------------------------------------------------------------------ AudioTap format
UI_VOICES = [60, 61, 62, 63]                          # UiAudio round-robin source ids


def build_tap_take(tap_dir, take, sr):
    """The synthetic take re-spelled as StoreGifApi's audioLog writes it (AUDIO.md 12): header clock "virtual", rec
    phases / speeds the game's own (here: the port's, so the take must pass the port check), events named
    tool/*, loop/*, rec/*, ui/*, pre/*, with "at", "scale" and "start"."""
    if os.path.isdir(tap_dir):
        shutil.rmtree(tap_dir)
    shutil.copytree(take, tap_dir)
    t0 = F.Take(take)
    trace = []
    F._plan_machines(t0, np.random.default_rng(1), t0.lead(), False, lambda s: 0.85, [], trace)
    shots = {}
    for v in F._plan_oneshots(t0, 0.0, lambda s: 0.0 if s.get("muted") else 0.85, 1.0):
        shots.setdefault(int(round(v.t_start / t0.dt)), []).append(v)
    with open(os.path.join(take, "audio.jsonl"), encoding="utf-8") as fh:
        lines = [json.loads(l) for l in fh.read().splitlines()]
    hdr = dict(lines[0], clock="virtual", dspBase=1000.0, dspBuffer=1024, sampleRate=48000, stepDt=0.01666666753590107)
    out, prev, ui_n, rattle_n = [json.dumps(hdr)], {}, 0, 0
    for i, o in enumerate(lines[1:]):
        st, s, t = o["state"], o["s"], o["s"] * t0.dt
        st["rec"] = {k: {"phase": ph, "speed": round(sp, 3), "leadMs": 42.0, "voices": []} for k, (ph, sp) in trace[i].items()}
        ev = []
        if s == 1:
            ev.append({"src": "pre/ContactA", "kind": "playing", "clip": "hose_pressurewasher_loop", "vol": 0.3, "scale": 1,
                       "pitch": 1, "delay": 0, "at": t, "start": 0.4, "pan": 0, "loop": True, "id": 3})
        for e in o["ev"]:
            e = dict(e, at=t + e.get("delay", 0.0), scale=1.0, start=0.0)
            if e["src"] == "ToolAudio":
                ev.append(dict(e, src="tool/rattle", id=40 + rattle_n % 14))
                rattle_n += 1
            elif e["src"] == "UiAudio":
                sid = UI_VOICES[ui_n % len(UI_VOICES)]
                ui_n += 1
                ev.append({"src": "ui/voice", "kind": "stop", "clip": "", "vol": 1, "scale": 1, "pitch": 1, "delay": 0,
                           "at": t, "start": 0, "pan": 0, "loop": False, "id": sid})
                ev.append(dict(e, src="ui/Tick", id=sid, scale=e["vol"]))
            else:
                ev.append(e)
        for v in shots.get(s, []):                    # tool/* one-shots: the OneShot source is at volume 0
            ev.append({"src": "tool/" + v.info["why"].split(",")[0].replace(" ", ""), "kind": "oneshot", "clip": v.clip,
                       "vol": 0.0, "scale": v.gain, "pitch": 1, "delay": 0, "at": t, "start": 0, "pan": 0, "loop": False, "id": 5})
        for k, sid in (("motor", 1), ("contact", 2)):  # loop/* play / stop, as LoopLayer.Tick taps them
            was, now = prev.get(k + "Vol", 0.0), st[k + "Vol"]
            if now > 0 and was <= 0:
                ev.append({"src": "loop/%sA" % k.capitalize(), "kind": "play", "clip": st[k + "Clip"], "vol": now, "scale": 1,
                           "pitch": st[k + "Pitch"], "delay": 0, "at": t, "start": 0.25, "pan": 0, "loop": True, "id": sid})
            elif was > 0 and now <= 0:
                ev.append({"src": "loop/%sA" % k.capitalize(), "kind": "stop", "clip": prev[k + "Clip"], "vol": 0, "scale": 1,
                           "pitch": 1, "delay": 0, "at": t, "start": 0, "pan": 0, "loop": True, "id": sid})
        for name, ph in st["rec"].items():            # rec/* scheduled at level 0 (Play sets volume 0)
            if ph["phase"] != (lines[i]["state"].get("rec", {}).get(name, {}).get("phase") if i else "Off"):
                ev.append({"src": "rec/Recorded%s/start" % name, "kind": "scheduled", "clip": "x_unresolvable_clip", "vol": 0.5,
                           "scale": 1, "pitch": 1, "delay": 0.042, "at": t + 0.042, "start": 0, "pan": 0, "loop": False, "id": 20})
        prev = st
        out.append(json.dumps({"type": "step", "s": s, "t": round(t, 6), "state": st, "src": [], "ev": ev}))
    out.append(json.dumps({"type": "end", "s": lines[-1]["s"], "t": lines[-1]["s"] * t0.dt, "aborted": False, "ev": [],
                           "sources": {}}))
    with open(os.path.join(tap_dir, "audio.jsonl"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(out) + "\n")



def tap_test(out_dir, take, sr):
    """The same take in the capture format must render the same machines and loops, the logged rattle only (the
    virtual clock drops nothing the game would play), no loop / rec / pre event voices, silent tool one-shots unless
    oneshot_source_volume, UI stops cutting by source id, and pass the port check (then fail it once corrupted)."""
    tap = os.path.join(out_dir, "take_tap")
    build_tap_take(tap, take, sr)
    pt = F.build_plan(tap, sr)
    pc = F.build_plan(take, sr)
    t1 = pc.take.duration + 6.0
    _mc, sc = F.render_take(take, 0.0, t1, sr, return_stems=True)
    mt, st = F.render_take(tap, 0.0, t1, sr, return_stems=True)
    F.write_wav(os.path.join(out_dir, "foley_tap.wav"), mt, sr)
    same = {k: float(np.max(np.abs(sc[k] - st.get(k, 0 * sc[k])))) for k in sc if k.startswith(("machine_", "motor", "contact"))}
    layers = {}
    for v in pt.voices:
        layers[v.layer] = layers.get(v.layer, 0) + 1
    ui = [v for v in pt.voices if v.layer == "ui"]
    first_ui_cut = ui[0].t_end if ui else None
    logged_at = sorted(e["at"] for o in pt.take.steps for e in o["ev"] if e.get("src") == "tool/rattle")
    played_at = sorted(v.t_start for v in pt.voices if v.layer == "rattle")
    rattle_at_ok = len(logged_at) == len(played_at) and all(abs(a - b) < 1e-9 for a, b in zip(logged_at, played_at))
    p1 = F.build_plan(tap, sr, oneshot_source_volume=1.0)
    # corrupt one Step's logged phase: the port check must say so
    bad = os.path.join(out_dir, "take_tap_bad")
    if os.path.isdir(bad):
        shutil.rmtree(bad)
    shutil.copytree(tap, bad)
    with open(os.path.join(bad, "audio.jsonl"), encoding="utf-8") as fh:
        lines = fh.read().splitlines()
    o = json.loads(lines[100])
    o["state"]["rec"]["DryVacuum"]["phase"] = "Stopping"
    lines[100] = json.dumps(o)
    with open(os.path.join(bad, "audio.jsonl"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines) + "\n")
    pb = F.build_plan(bad, sr)
    return {"virtualClock": pt.take.virtual_clock, "voices": layers, "warnings": pt.warnings,
            "stemsVsContractMaxAbsDiff": same, "nan": int(np.count_nonzero(~np.isfinite(mt))),
            "peakDbfs": round(20 * math.log10(float(np.max(np.abs(mt))) + 1e-12), 2),
            "firstUiVoiceCutAt": None if first_ui_cut is None or not math.isfinite(first_ui_cut) else round(first_ui_cut, 4),
            "fifthUiCallAt": round(ui[4].t_start, 4) if len(ui) > 4 else None, "rattleAtLoggedAt": rattle_at_ok,
            "oneshotsWithSourceVolume1": sum(v.layer == "oneshot" for v in p1.voices),
            "corruptedPhaseWarnings": [w for w in pb.warnings if "disagrees" in w]}



def main(argv):
    out_dir = os.path.abspath(argv[0] if argv else "foley_test")
    take = os.path.join(out_dir, "take")
    build_take(take)
    sr = 48000
    # the power / switch one-shots at full source volume, so their attacks are measured too (the game's OneShot source
    # is at volume 0: by default they are silent, checked below)
    O = {"oneshot_source_volume": 1.0}
    plan = F.build_plan(take, sr, **O)
    t1 = plan.take.duration + 6.0
    mix, stems = F.render_take(take, 0.0, t1, sr, return_stems=True, **O)
    F.write_wav(os.path.join(out_dir, "foley_full.wav"), mix, sr)
    for k, y in stems.items():
        F.write_wav(os.path.join(out_dir, "stems", k + ".wav"), y, sr)
    win = F.render_take(take, 9.0, 12.5, sr, **O)
    F.write_wav(os.path.join(out_dir, "foley_window.wav"), win, sr)
    ref = mix[int(round(9.0 * sr)):int(round(12.5 * sr))]

    rep = {"seconds": round(len(mix) / sr, 3), "nan": int(np.count_nonzero(~np.isfinite(mix))),
           "peakDbfs": round(20 * math.log10(float(np.max(np.abs(mix))) + 1e-12), 2),
           "rmsDbfs": round(20 * math.log10(float(np.sqrt(np.mean(mix.astype(np.float64) ** 2))) + 1e-12), 2),
           "loudness": ebur128(os.path.join(out_dir, "foley_full.wav")),
           "windowVsFullMaxAbsDiff": float(np.max(np.abs(win - ref))),
           "voices": {}, "warnings": plan.warnings,
           "machines": {k: m.log for k, m in plan.machines.items()},
           "stitchExact": stitch_exact(plan, stems, sr)}
    for v in plan.voices:
        rep["voices"][v.layer] = rep["voices"].get(v.layer, 0) + 1
    worst = {}
    for t, kind, layer in seams(plan):
        if t <= 0.01 or t >= t1 - 0.2 or kind.startswith("onset"):
            continue
        for name, x in (("stem", stems[layer]), ("mix", mix)):
            r = jump_ratio(x, sr, t)
            if r is None or r["level"] < 1e-4:
                continue
            key = kind.split(":")[0] + "/" + name
            if key not in worst or r["ratio"] > worst[key]["ratio"]:
                worst[key] = {"ratio": round(r["ratio"], 2), "jumpOverRms": round(r["overRms"], 3), "t": round(t, 4),
                              "what": kind, "layer": layer, "levelDb": round(20 * math.log10(r["level"]), 1)}
    rep["seamWorst"] = dict(sorted(worst.items()))
    rep["onsetWorst"] = onsets(plan, stems, sr)
    rep["spikes"] = {k: spike_scan(y, sr) for k, y in sorted(stems.items())}
    rep["spikes"]["_mix"] = spike_scan(mix, sr)
    # the fallbacks: rattle rolled from state.rattleScheduled, one-shots forced, a take that opens mid-stroke
    p2 = F.build_plan(take, sr, emulate_rattle=True, emulate_oneshots=True, **O)
    p0 = F.build_plan(take, sr)
    rep["fallback"] = {"rattleVoices": sum(v.layer == "rattle" for v in p2.voices),
                       "oneshotVoices": sum(v.layer == "oneshot" for v in p2.voices),
                       "oneshotVoicesAsInGame": sum(v.layer == "oneshot" for v in p0.voices)}
    cut = os.path.join(out_dir, "take_midstroke")
    if os.path.isdir(cut):
        shutil.rmtree(cut)
    os.makedirs(cut)
    with open(os.path.join(take, "audio.jsonl"), encoding="utf-8") as fh:
        lines = fh.read().splitlines()
    kept = [lines[0]] + [l for l in lines[1:] if 60 <= json.loads(l)["s"] <= 150]   # 1.0-2.5 s: dry vac held
    with open(os.path.join(cut, "audio.jsonl"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(kept) + "\n")
    for warm in (False, True):
        y = F.render_take(cut, 1.0, 2.5, sr, warm=warm)
        rms = lambda a, b: round(20 * math.log10(float(np.sqrt(np.mean(y[int(a * sr):int(b * sr)] ** 2))) + 1e-12), 1)
        pw = F.build_plan(cut, sr, warm=warm)
        rep["fallback"]["warm" if warm else "cold"] = {"log": pw.machines["DryVacuum"].log[:2],
                                                       "rmsDb_first100ms": rms(0.0, 0.1), "rmsDb_last500ms": rms(1.0, 1.5)}
    rep["tapFormat"] = tap_test(out_dir, take, sr)
    print(json.dumps(rep, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
