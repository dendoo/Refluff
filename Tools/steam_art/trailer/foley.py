"""Offline foley for StoreGifApi trailer takes (Docs/TRAILER.md 7; notes in README_foley.md).

The takes are silent: StoreGifApi mutes the Editor and steps frames at its own pace, and the unfocused Editor's audio
clock stands still. What a take logs is <take>/audio.jsonl (AUDIO.md 12, STORE_CLIPS.md 5): per Step,
ToolAudio.DebugStateJson() plus every AudioTap event. With StoreGifApi's audioLog the dsp-timed audio runs on a virtual
clock (header "clock":"virtual"), so RecordedMachine's logged phases are the game's own; an older log on the frozen real
clock shows them stuck in "Starting". This module rebuilds the game's own mix from the log and the game's own clips:

  LoopLayer motor / contact  the logged per-Step volume, pitch and low-pass cutoff of the playing source; a random
                             entry point per (re)start of a clip over 1 s, the crossfading old source on a clip change
                             decaying at volumeEaseHz; Unity's AudioLowPassFilter as a 24 dB/oct low-pass.
  RecordedMachine            a port of RecordedMachine.cs on Step time, fed only with what the log says (tool, recorded,
                             machineOn, speed01): spin-up -> sample-exact stitch into the loop -> coast entered at the
                             current speed, equal-power crossfades, 5 voices, 42 ms lead; checked Step by Step against
                             the logged rec phases / speeds on a virtual-clock take.
  ToolAudio one-shots        the logged events (tool/powerOn|powerOff|switch); on an old log without any, re-derived from
                             state changes. At the OneShot source's volume, which is 0 in the game (silent clicks).
  crumb rattle               the logged scheduled events (tool/rattle); without any, one strike per rattleScheduled
                             increment (+ the strikes a frozen-clock take dropped).
  everything else            every other event (ui/<UiSfx>, ...) as logged: clip, volume, pitch, pan, at / delay.

API (the editing engine imports it):
    render_take(take_dir, t0, t1, sr=48000, **opts) -> float32 (n, 2)   take time 0 = SeqBegin, t(step s) = s*stepDt
    resolve_clip(name, take_dir=None) -> path
    load_wav(path, sr=48000) -> float32 (n, 2), resampled with a polyphase Kaiser filter
    build_plan(take_dir, sr=48000, **opts) -> Plan (voices, warnings) - what render_take plays
CLI:
    python Tools/steam_art/trailer/foley.py TAKE_DIR OUT.wav [--t0 S] [--t1 S] [--tail S] [--stems DIR]
                                            [--seed N] [--warm] [--lead-ms MS] [--latency-ms MS] [--oneshot-volume V]
"""
import argparse
import json
import math
import os
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass, field

import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly, sosfilt

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
ASSETS = os.path.join(ROOT, "Assets")
RECORDED_DIR = os.path.join(ASSETS, "CarpetCleaning", "Audio", "Recorded")
BANK_ASSET = os.path.join(ASSETS, "CarpetCleaning", "Settings", "ToolSoundBank.asset")
SCENE = os.path.join(ASSETS, "Scenes", "SampleScene.unity")
AUDIO_EXT = (".wav", ".ogg", ".aif", ".aiff", ".mp3", ".flac")

# ToolId order (Scripts/ToolId.cs) and SfxRecipe order (ToolSoundBank.cs)
TOOLS = ["Scraper", "DryVacuum", "Hose", "FoamSprayer", "Brush", "Squeegee", "WetVacuum", "Dryer", "FloorCleaner", "Mop"]
RECIPES = ["None", "VacuumMotor", "WetVacuumMotor", "Blower", "WaterJet", "SprayHiss", "FoamHiss", "Scrub", "Crunch",
           "Squeegee", "Slop", "Slurp", "Friction", "RotaryMotor"]

# ToolAudio / RecordedMachine numbers: keep in step with the C#
OPEN_HZ = 22000.0            # ToolAudio.OpenHz
VOLUME_EASE_HZ = 14.0        # ToolAudio.volumeEaseHz (SampleScene): also the rate an old crossfade source dies at
STOP_LEVEL = 0.0008          # LoopLayer.Tick: below this with a zero target the source stops
XFADE_LEVEL = 0.001          # LoopLayer.SetClip: a clip change crossfades only above this volume
DEFAULT_LEAD_S = 0.042       # RecordedMachine.Lead(): 1.5 blocks + 10 ms at 1024 samples / 48 kHz
LOAD_EASE_HZ = 3.0           # RecordedMachine: _loadPitch ease
MACHINE_VOICES = 5           # RecordedMachine(host, name, voices = 5)
LPF_RESONANCE_Q = 1.0        # LoopLayer.AddLowPass: lowpassResonanceQ
HARD_STOP_FADE_S = 0.003     # ours: a Stop() mid-waveform gets a 3 ms ramp instead of a click
RATTLE_MAX_PER_FRAME = 8     # ToolAudio.rattleMaxPerFrame
# ToolAudio's OneShot source (power on / off, tool-switch tick) is made by LoopLayer.MakeSource, which sets volume 0, and
# nothing raises it: Unity scales a PlayOneShot by its source's volume, so the game plays those clicks SILENT (AUDIO.md
# 12; the tap logs them with vol 0 and the requested `scale`). build_plan(oneshot_source_volume=1.0) = as if fixed.
ONESHOT_SOURCE_VOLUME = 0.0
INF = float("inf")
# A mono clip on a 2D source: full level on both speakers, as the project's levelling assumes (cut_machine_recording.py
# loudness(): "a mono clip counts as played on both speakers"). 0.7071 would model a constant-power centre pan instead.
MONO_GAIN = 1.0


# ------------------------------------------------------------------------------------------------ WAV files
_native_cache = {}
_hq_cache = {}


def _decode(path):
    """(rate, float32 (n, 2)) of any audio file; non-WAV goes through ffmpeg."""
    if not path.lower().endswith(".wav"):
        sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
        from clip_probe import ffmpeg_exe  # Tools/steam_art/clip_probe.py
        tmp = os.path.join(tempfile.gettempdir(), "foley_%d.wav" % abs(hash(path)))
        subprocess.run([ffmpeg_exe(), "-y", "-v", "error", "-i", path, "-c:a", "pcm_f32le", tmp], check=True)
        path = tmp
    rate, d = wavfile.read(path)
    if d.dtype == np.int16:
        x = d.astype(np.float32) / 32768.0
    elif d.dtype == np.int32:
        x = d.astype(np.float32) / 2147483648.0
    elif d.dtype == np.uint8:
        x = (d.astype(np.float32) - 128.0) / 128.0
    else:
        x = d.astype(np.float32)
    if x.ndim == 1:
        x = np.stack([x, x], axis=1) * MONO_GAIN
    elif x.shape[1] == 1:
        x = np.repeat(x, 2, axis=1)
    elif x.shape[1] > 2:
        x = x[:, :2]
    return int(rate), np.ascontiguousarray(x, dtype=np.float32)


def load_native(path):
    """(rate, float32 (n, 2)) at the file's own rate, cached."""
    key = (os.path.abspath(path), os.path.getmtime(path))
    if key not in _native_cache:
        _native_cache[key] = _decode(path)
    return _native_cache[key]


def load_wav(path, sr=48000):
    """float32 (n, 2) at `sr`; resampled with scipy's polyphase Kaiser-windowed filter when the rates differ."""
    key = (os.path.abspath(path), os.path.getmtime(path), int(sr))
    if key not in _hq_cache:
        rate, x = load_native(path)
        if rate != sr:
            g = math.gcd(rate, int(sr))
            x = np.ascontiguousarray(resample_poly(x, int(sr) // g, rate // g, axis=0), dtype=np.float32)
        _hq_cache[key] = x
    return _hq_cache[key]


# ------------------------------------------------------------------------------------------------ clip names
_index = None


def _asset_index():
    """(clip name lower -> path, meta guid -> path) for every audio file under Assets/. Unity names a clip after its file."""
    global _index
    if _index is None:
        by_name, by_guid = {}, {}
        for d, _dirs, files in os.walk(ASSETS):
            for f in files:
                low = f.lower()
                if low.endswith(AUDIO_EXT):
                    p = os.path.join(d, f)
                    stem = os.path.splitext(f)[0].lower()
                    # the Audio folder wins over a stray copy elsewhere
                    if stem not in by_name or "carpetcleaning" in p.lower():
                        by_name[stem] = p
                elif low.endswith(".meta") and low[:-5].endswith(AUDIO_EXT):
                    try:
                        with open(os.path.join(d, f), encoding="utf-8", errors="replace") as fh:
                            for _ in range(4):
                                m = re.match(r"guid:\s*([0-9a-f]{32})", fh.readline().strip())
                                if m:
                                    by_guid[m.group(1)] = os.path.join(d, f[:-5])
                                    break
                    except OSError:
                        pass
        _index = (by_name, by_guid)
    return _index


def clip_for_guid(guid):
    """Asset path of the audio file with this meta guid, or None."""
    return _asset_index()[1].get(guid) if guid else None


def clip_name(path):
    return os.path.splitext(os.path.basename(path))[0] if path else None


def _procedural_alias(name):
    """ToolAudio's runtime placeholder names -> the copies ToolSoundBaker baked into Audio/Generated."""
    if name == "sfx_tool_switch":
        return "ui_tool_switch"
    m = re.match(r"sfx_([A-Za-z]+)_(on|off)$", name) or re.match(r"sfx_([A-Za-z]+)_(debris_\d+)$", name)
    if m:
        return "tool_%s_%s" % (m.group(1).lower(), m.group(2))
    m = re.match(r"sfx_([A-Za-z]+)_([A-Za-z]+)$", name)
    if m and m.group(1) in TOOLS and m.group(2) in RECIPES:
        e = bank().get("entries", {}).get(TOOLS.index(m.group(1)), {})
        slot = "motor" if int(e.get("motorRecipe", -1)) == RECIPES.index(m.group(2)) else "contact"
        return "tool_%s_%s" % (m.group(1).lower(), slot)
    return None


_take_maps = {}


def _take_clip_map(take_dir):
    p = os.path.join(take_dir, "audio_clips.json")
    if not os.path.isfile(p):
        return {}
    key = (os.path.abspath(p), os.path.getmtime(p))
    if key not in _take_maps:
        with open(p, encoding="utf-8") as fh:
            _take_maps[key] = json.load(fh)
    return _take_maps[key]


def resolve_clip(name, take_dir=None):
    """Path of the AudioClip called `name`: the take's audio_clips.json first ({"asset": "Assets/..."} relative to the
    project, {"wav": "_clips/x.wav"} relative to the take), then any audio file of that name under Assets/, then the
    baked copy of a runtime placeholder (sfx_*). FileNotFoundError when nothing matches."""
    if not name:
        raise FileNotFoundError("empty clip name")
    if take_dir:
        e = _take_clip_map(take_dir).get(name)
        if isinstance(e, dict):
            for k, base in (("wav", take_dir), ("asset", ROOT), ("path", ROOT)):
                if e.get(k):
                    p = e[k] if os.path.isabs(e[k]) else os.path.join(base, e[k])
                    if os.path.isfile(p):
                        return os.path.normpath(p)
    by_name = _asset_index()[0]
    p = by_name.get(name.lower())
    if p:
        return p
    alias = _procedural_alias(name)
    if alias and by_name.get(alias.lower()):
        return by_name[alias.lower()]
    raise FileNotFoundError("no clip named '%s'" % name)


# ------------------------------------------------------------------------------------------------ the sound bank
def _yaml_value(v):
    v = v.strip()
    if v.startswith("{"):
        m = re.search(r"guid:\s*([0-9a-f]{32})", v)
        return m.group(1) if m else None          # {fileID: 0} = an empty slot
    try:
        return float(v)
    except ValueError:
        return v.strip("'\"")


_bank = {}


def bank(path=BANK_ASSET):
    """Settings/ToolSoundBank.asset as a dict: top-level fields plus entries {tool index: {field: value}}. Clip slots
    hold meta guids; recording curves are [[time, value], ...]. A narrow line parser for this one Unity YAML file."""
    key = (path, os.path.getmtime(path) if os.path.isfile(path) else 0)
    if key in _bank:
        return _bank[key]
    out = {"masterVolume": 0.85, "rattlePitch": 1.0, "rattleVolume": 1.0, "entries": {}}
    if not os.path.isfile(path):
        _bank[key] = out
        return out
    cur, ctx, curve = None, None, None
    with open(path, encoding="utf-8") as fh:
        for raw in fh:
            line = raw.rstrip("\r\n")
            s = line.strip()
            ind = len(line) - len(line.lstrip(" "))
            if line.startswith("  - tool: "):
                cur = {"tool": int(float(s.split(":", 1)[1])), "recording": {"startSpeed": [], "stopSpeed": []},
                       "debrisClips": []}
                out["entries"][cur["tool"]] = cur
                ctx = curve = None
            elif cur is None:
                if ind == 2 and ":" in s:
                    k, v = s.split(":", 1)
                    if k != "entries":
                        out[k] = _yaml_value(v)
            elif ind == 4 and s.startswith("- ") and ctx == "debris":
                cur["debrisClips"].append(_yaml_value(s[2:]))
            elif ind == 4 and ":" in s:
                k, v = s.split(":", 1)
                ctx = "rec" if k == "recording" else "debris" if k == "debrisClips" else None
                if ctx is None:
                    cur[k] = _yaml_value(v)
            elif ctx == "rec" and ind == 6 and ":" in s:
                k, v = s.split(":", 1)
                if k in ("startSpeed", "stopSpeed"):
                    curve = cur["recording"][k]
                else:
                    curve = None
                    cur["recording"][k] = _yaml_value(v)
            elif curve is not None and s.startswith("time:"):
                curve.append([float(s[5:]), 0.0])
            elif curve is not None and s.startswith("value:") and curve:
                curve[-1][1] = float(s[6:])
    _bank[key] = out
    return out


def entry(tool):
    """The bank entry of a tool (name or index); the silent default when missing, as ToolSoundBank.Get."""
    i = TOOLS.index(tool) if isinstance(tool, str) else int(tool)
    return bank()["entries"].get(i, {"tool": i, "recording": {"startSpeed": [], "stopSpeed": []}, "debrisClips": []})


def tool_audio_volume():
    """ToolAudio.volume as the scene holds it (1 when it cannot be read)."""
    try:
        with open(SCENE, encoding="utf-8") as fh:
            txt = fh.read()
        i = txt.index("CarpetCleaning.ToolAudio\n")
        m = re.search(r"\n  volume: ([0-9.]+)", txt[i:i + 600])
        return float(m.group(1)) if m else 1.0
    except (OSError, ValueError):
        return 1.0


# ------------------------------------------------------------------------------------------------ machine takes
@dataclass
class MachineRec:
    """One MachineRecording: three clip names, both speed curves (linear keys, as ToolSoundBaker.LinearCurve builds
    them) and the crossfade. Lengths are clip.length (samples / frequency of the file)."""
    start: str
    loop: str
    stop: str
    start_t: np.ndarray
    start_v: np.ndarray
    stop_t: np.ndarray
    stop_v: np.ndarray
    crossfade: float = 0.05
    start_len: float = 0.0
    loop_len: float = 0.0
    stop_len: float = 0.0
    source: str = ""

    def start_speed(self, t):
        return float(np.interp(t, self.start_t, self.start_v))

    def stop_speed(self, t):
        return float(np.interp(t, self.stop_t, self.stop_v))


_cut_index = None


def _machine_cuts():
    """clip name lower -> cut JSON (Tools/cut_machine_recording.py) for every start / loop / stop clip in Recorded/."""
    global _cut_index
    if _cut_index is None:
        _cut_index = {}
        if os.path.isdir(RECORDED_DIR):
            for f in sorted(os.listdir(RECORDED_DIR)):
                if not f.endswith(".json"):
                    continue
                try:
                    with open(os.path.join(RECORDED_DIR, f), encoding="utf-8") as fh:
                        d = json.load(fh)
                except (OSError, ValueError):
                    continue
                if all(k in d for k in ("startFile", "loopFile", "stopFile", "startSpeedT", "stopSpeedT")):
                    for k in ("startFile", "loopFile", "stopFile"):
                        _cut_index[os.path.splitext(d[k])[0].lower()] = os.path.join(RECORDED_DIR, f)
    return _cut_index


_credit_index = None


def clip_credit(name):
    """The credit line of a recorded clip (the cut JSON next to it in Audio/Recorded/), or None."""
    global _credit_index
    if _credit_index is None:
        _credit_index = {}
        if os.path.isdir(RECORDED_DIR):
            for f in sorted(os.listdir(RECORDED_DIR)):
                if not f.endswith(".json"):
                    continue
                try:
                    with open(os.path.join(RECORDED_DIR, f), encoding="utf-8") as fh:
                        d = json.load(fh)
                except (OSError, ValueError):
                    continue
                for k in ("file", "startFile", "loopFile", "stopFile"):
                    if isinstance(d, dict) and d.get(k) and d.get("credit"):
                        _credit_index[os.path.splitext(os.path.basename(d[k]))[0].lower()] = d["credit"]
    return _credit_index.get(str(name or "").lower())


def _clip_seconds(name, take_dir):
    rate, x = load_native(resolve_clip(name, take_dir))
    return len(x) / float(rate)


def machine_recording(tool, take_dir=None, seen_names=()):
    """The MachineRecording a take played for `tool`, resolved by name: a clip name the take's state showed in
    rec.<tool>.voices first (the dry vac may be Samsung or Dyson), else the bank's recording slots. None when the tool
    has no complete recording."""
    e = entry(tool)
    r = e.get("recording", {})
    cuts = _machine_cuts()
    jpath = None
    for n in seen_names:
        jpath = jpath or cuts.get(str(n).lower())
    if jpath is None:
        for slot in ("start", "loop", "stop"):
            jpath = jpath or cuts.get((clip_name(clip_for_guid(r.get(slot))) or "").lower())
    xf = float(r.get("crossfade", 0.05) or 0.05)
    if jpath:
        with open(jpath, encoding="utf-8") as fh:
            d = json.load(fh)
        m = MachineRec(clip_name(d["startFile"]), clip_name(d["loopFile"]), clip_name(d["stopFile"]),
                       np.asarray(d["startSpeedT"], float), np.asarray(d["startSpeedV"], float),
                       np.asarray(d["stopSpeedT"], float), np.asarray(d["stopSpeedV"], float), xf,
                       source=os.path.basename(jpath))
    else:
        names = [clip_name(clip_for_guid(r.get(k))) for k in ("start", "loop", "stop")]
        ss, sp = r.get("startSpeed") or [], r.get("stopSpeed") or []
        if not all(names) or len(ss) < 2 or len(sp) < 2:
            return None
        m = MachineRec(names[0], names[1], names[2], np.array([k[0] for k in ss]), np.array([k[1] for k in ss]),
                       np.array([k[0] for k in sp]), np.array([k[1] for k in sp]), xf, source="ToolSoundBank.asset")
    try:
        m.start_len = _clip_seconds(m.start, take_dir)
        m.loop_len = _clip_seconds(m.loop, take_dir)
        m.stop_len = _clip_seconds(m.stop, take_dir)
    except FileNotFoundError:
        return None
    return m


# ------------------------------------------------------------------------------------------------ the take log
class Take:
    """<take>/audio.jsonl: a header line, then one line per Step {s, t, state, ev}. t(step) = s * stepDt; a stepDt
    within 1e-4 of 1/fps is taken as exactly 1/fps, so Step s lands on video frame s."""

    def __init__(self, take_dir):
        self.dir = os.path.abspath(take_dir)
        self.path = os.path.join(self.dir, "audio.jsonl")
        self.header, self.steps = {}, []
        with open(self.path, encoding="utf-8") as fh:
            for line in fh:
                line = line.strip()
                if not line:
                    continue
                o = json.loads(line)
                if o.get("type") == "header":
                    self.header = o
                elif o.get("type", "step") == "step" and "s" in o:
                    self.steps.append(o)
        self.steps.sort(key=lambda o: o["s"])
        fps = float(self.header.get("fps") or 60)
        dt = float(self.header.get("stepDt") or 1.0 / fps)
        self.dt = 1.0 / fps if abs(dt - 1.0 / fps) < 1e-4 else dt
        for o in self.steps:
            o["t"] = o["s"] * self.dt
            st = o.get("state") or {}
            o["state"] = json.loads(st) if isinstance(st, str) else st     # DebugStateJson() embedded as a string
            o["ev"] = o.get("ev") or []
        self.duration = self.steps[-1]["t"] if self.steps else 0.0
        # StoreGifApi's audioLog runs the dsp-timed audio (RecordedMachine, the crumb rattle) on a virtual clock that
        # advances with the Steps (AUDIO.md 12): the logged phases, speeds and rattle counters are then the game's own
        self.virtual_clock = self.header.get("clock") == "virtual"

    def rec_names(self, tool):
        """Clip names the state ever showed on this tool's RecordedMachine voices."""
        seen = []
        for o in self.steps:
            for v in ((o["state"].get("rec") or {}).get(tool) or {}).get("voices") or []:
                if v.get("clip") and v["clip"] not in seen:
                    seen.append(v["clip"])
        return seen

    def lead(self):
        """RecordedMachine.Lead(): from the header's dspBuffer / sampleRate (exact), else the leadMs of any machine
        (1 decimal), else 42 ms."""
        buf, rate = self.header.get("dspBuffer"), self.header.get("sampleRate")
        if isinstance(buf, (int, float)) and isinstance(rate, (int, float)) and buf > 0 and rate > 0:
            return min(0.1, max(0.02, 1.5 * buf / max(8000.0, rate) + 0.01))
        for o in self.steps:
            for m in (o["state"].get("rec") or {}).values():
                if isinstance(m, dict) and m.get("leadMs"):
                    return float(m["leadMs"]) / 1000.0
        return DEFAULT_LEAD_S


# ------------------------------------------------------------------------------------------------ voices
@dataclass
class Voice:
    """One clip playing on one source: what the renderer mixes. Times are take seconds."""
    layer: str
    clip: str
    loop: bool
    t_start: float                 # the moment the clip is at `offset`
    offset: float = 0.0            # clip seconds
    rate: float = 1.0              # playback rate (AudioSource.pitch) while rate_t is None
    rate_t: list = None            # piecewise-linear rate knots
    rate_v: list = None
    gain: float = 1.0              # AudioSource.volume while gain_t is None
    gain_t: list = None
    gain_v: list = None
    fade_in: float = 0.0           # equal-power from t_start (RecordedMachine.Voice.Gain)
    fade_out_at: float = INF
    fade_out: float = 0.0
    t_end: float = INF             # hard end (a Stop(), SetScheduledEndTime)
    end_fade: float = 0.0          # linear ramp before t_end; 0 = sample-exact (the stitch into the loop)
    cut_t: list = None             # AudioLowPassFilter cutoff knots (Hz); None = no filter on this source
    cut_v: list = None
    pan: float = 0.0               # AudioSource.panStereo
    tail_clip: str = None          # clip whose first samples follow this one (interpolation across the stitch)
    id: object = None
    kind: str = ""
    info: dict = field(default_factory=dict)


# ------------------------------------------------------------------------------------------------ RecordedMachine
OFF, STARTING, RUNNING, STOPPING = "Off", "Starting", "Running", "Stopping"


class _MVoice:
    """RecordedMachine.Voice, one per Play (the pool slot only counts busy voices), plus what the renderer needs."""

    def __init__(self, role, loop, offset, at, pitch, fade_in, volume):
        self.role, self.loop = role, loop
        self.busy = True
        self.at, self.end = at, INF
        self.offset, self.pitch, self.fade_in = offset, pitch, fade_in
        self.fade_out_at, self.fade_out = INF, 0.0
        self.released = INF
        self.rate_t, self.rate_v = [at], [pitch]
        self.vol_t, self.vol_v = [at], [volume]

    def position(self, now):
        return self.offset + max(0.0, now - self.at) * self.pitch

    def gain(self, now):
        g = math.sin(0.5 * math.pi * min(1.0, max(0.0, (now - self.at) / self.fade_in))) if self.fade_in > 0 else 1.0
        if now > self.fade_out_at:
            g *= math.cos(0.5 * math.pi * min(1.0, max(0.0, (now - self.fade_out_at) / max(1e-3, self.fade_out))))
        return g


def _time_at_speed(t_keys, v_keys, length, speed, rising):
    """RecordedMachine.TimeAtSpeed: the first clip time (5 ms steps) where the curve reaches `speed`, else the end."""
    t = np.arange(0.0, length, 0.005)
    if len(t) == 0:
        return length
    v = np.interp(t, t_keys, v_keys)
    hit = np.nonzero(v >= speed if rising else v <= speed)[0]
    return float(t[hit[0]]) if len(hit) else float(length)


class Machine:
    """RecordedMachine.cs with Step time as the audio clock. Tick() is called once per Step, as ToolAudio.TickRecorded
    does for every machine it has created."""

    def __init__(self, tool, lead):
        self.tool, self.lead = tool, lead
        self.slots = [None] * MACHINE_VOICES
        self.start = self.loop = self.stop = None
        self.rec = None
        self.phase = OFF
        self.speed = 0.0
        self.load = 1.0
        self.volume = 0.0
        self.voices = []            # every _MVoice ever played, for the renderer
        self.log = []               # (time, what) transitions, for tests

    # ---------------------------------------------------------------- per Step
    def tick(self, now, on, usable, rec, volume, pitch, load_pitch, dt):
        self.volume = volume
        pitch = min(2.5, max(0.25, pitch))
        if not usable or rec is None:
            if self.phase != OFF:
                self._fade_out_all(now, 0.05)
            self.phase = OFF
            self.start = self.loop = self.stop = None
        else:
            if self.phase == STARTING and self.loop is not None and now >= self.loop.at:
                self.phase, self.start = RUNNING, None
            if self.phase == STOPPING and self.stop is not None and now >= self.stop.end:
                self.phase, self.stop = OFF, None
            if on and self.phase in (OFF, STOPPING):
                self._switch_on(rec, now, pitch)
            elif not on and self.phase in (STARTING, RUNNING):
                self._switch_off(rec, now, pitch)
        self.speed = self._speed_at(now)
        target = load_pitch if self.phase == RUNNING else 1.0
        self.load += (target - self.load) * (1.0 - math.exp(-LOAD_EASE_HZ * max(0.0, dt)))
        for i, v in enumerate(self.slots):
            if v is None or not v.busy:
                continue
            if now > v.end + 0.1 or now > v.fade_out_at + v.fade_out + 0.05:
                self._release(v, now)
                continue
            # a value set this Step is heard from the next mix block: ramp to it over one Step, starting now (a ramp
            # that started before the tick would move the loop's rate ahead of the stitch)
            v.vol_t.append(now + dt)
            v.vol_v.append(min(1.0, max(0.0, volume)))
            if v is self.loop:
                v.rate_t.append(now + dt)
                v.rate_v.append(pitch * self.load)

    def warm_start(self, rec, now, pitch, offset):
        """Not in the C#: a take that opens with the machine already held starts it in the loop, not from rest."""
        self.rec = rec
        self.loop = self._play("loop", True, offset, now, pitch, 0.0, now)
        self.phase = RUNNING
        self.log.append((now, "warm"))

    # ---------------------------------------------------------------- transitions
    def _switch_on(self, rec, now, pitch):
        at = now + self.lead
        from_rest = self.phase == OFF
        offset = 0.0 if from_rest else _time_at_speed(rec.start_t, rec.start_v, rec.start_len, self._speed_at(at), True)
        self._fade_out_all(at, rec.crossfade)
        self.start = self.loop = self.stop = None
        self.rec = rec
        self.load = 1.0
        remaining = (rec.start_len - offset) / pitch
        if remaining <= max(0.05, rec.crossfade):
            self.loop = self._play("loop", True, 0.0, at, pitch, 0.0 if from_rest else rec.crossfade, now)
            self.phase = RUNNING
            self.log.append((now, "on->loop"))
            return
        self.start = self._play("start", False, offset, at, pitch, 0.0 if from_rest else rec.crossfade, now)
        self.start.end = at + remaining                                  # SetScheduledEndTime
        self.loop = self._play("loop", True, 0.0, at + remaining, pitch, 0.0, now)
        self.phase = STARTING
        self.log.append((now, "on@%.3f" % offset))

    def _switch_off(self, rec, now, pitch):
        at = now + self.lead
        speed = self._speed_at(at)
        if self.loop is not None and self.loop.at >= at:
            self._release(self.loop, now)                                 # still ahead of the mixer: cancelled
            self.loop = None
        offset = _time_at_speed(rec.stop_t, rec.stop_v, rec.stop_len, speed, False)
        self._fade_out_all(at, rec.crossfade)
        self.start = self.loop = None
        self.rec = rec
        self.stop = self._play("stop", False, offset, at, pitch, rec.crossfade, now)
        self.stop.end = at + (rec.stop_len - offset) / pitch
        self.phase = STOPPING
        self.log.append((now, "off@%.3f speed %.3f" % (offset, speed)))

    def _speed_at(self, t):
        r = self.rec
        if self.phase == STARTING:
            if self.start is not None and self.loop is not None and t < self.loop.at and r is not None:
                return min(1.0, max(0.0, r.start_speed(self.start.position(t))))
            return 1.0
        if self.phase == RUNNING:
            return 1.0
        if self.phase == STOPPING:
            return min(1.0, max(0.0, r.stop_speed(self.stop.position(t)))) if self.stop is not None and r else 0.0
        return 0.0

    # ---------------------------------------------------------------- voices
    def _play(self, role, loop, offset, at, pitch, fade_in, now):
        i = self._free_slot(now)                             # FreeVoice judges and Stop()s at Now, not at `at`
        v = _MVoice(role, loop, offset, at, pitch, fade_in, self.volume)
        v.clip = getattr(self.rec, role)                     # _switch_on / _switch_off set self.rec first
        v.tail = self.rec.loop if role == "start" else None  # the loop's head follows the spin-up's last sample
        self.slots[i] = v
        self.voices.append(v)
        return v

    def _fade_out_all(self, at, seconds):
        for v in self.slots:
            if v is not None and v.busy and v.fade_out_at > at:
                v.fade_out_at, v.fade_out = at, seconds

    def _free_slot(self, now):
        for i, v in enumerate(self.slots):
            if v is None or not v.busy:
                return i
        best, best_gain = None, INF
        for i, v in enumerate(self.slots):
            if v is self.start or v is self.loop or v is self.stop:
                continue
            g = v.gain(now)
            if g < best_gain:
                best, best_gain = i, g
        best = 0 if best is None else best
        self._release(self.slots[best], now)
        return best

    @staticmethod
    def _release(v, now):
        if v is not None and v.busy:
            v.busy = False
            v.released = min(v.released, now)

    def to_voices(self, layer):
        out = []
        for v in self.voices:
            name = v.clip
            stitched = v.role == "start" and v.end <= v.released
            out.append(Voice(layer, name, v.loop, v.at, v.offset, v.pitch, v.rate_t, v.rate_v, 1.0, v.vol_t, v.vol_v,
                             v.fade_in, v.fade_out_at, v.fade_out, min(v.end if v.role == "start" else INF, v.released),
                             0.0 if stitched else HARD_STOP_FADE_S, tail_clip=getattr(v, "tail", None),
                             kind="machine", info={"role": v.role, "tool": self.tool}))
        return out


# ------------------------------------------------------------------------------------------------ the plan
def _f(s, k, default=0.0):
    v = s.get(k)
    return float(v) if isinstance(v, (int, float)) and not isinstance(v, bool) and math.isfinite(v) else default


def _plan_loop_layer(take, prefix, layer, rng, latency, warnings):
    """ToolAudio.LoopLayer from the logged <prefix>Clip / Vol / Pitch / Cut of its playing source (Vol is already the
    eased value). A source starts when its volume rises from 0 (at a random point of a clip over 1 s), stops when it
    reads 0, and on a clip change the old one keeps its pitch and filter and decays at volumeEaseHz."""
    dt = take.dt
    k = 1.0 - math.exp(-VOLUME_EASE_HZ * dt)
    voices, cur, old, bad = [], None, None, set()
    prev_clip, prev_vol = "", 0.0
    for o in take.steps:
        s = o["state"]
        if "tool" not in s:
            continue
        t = o["t"] + latency
        clip = s.get(prefix + "Clip") or ""
        vol, pitch, cut = _f(s, prefix + "Vol"), _f(s, prefix + "Pitch", 1.0), _f(s, prefix + "Cut", OPEN_HZ)
        if clip != prev_clip and old is not None:
            # LoopLayer.SetClip: the source still fading from the last change is Stop()ped, whether it becomes the new
            # _cur (crossfade swap) or stays _old (no crossfade)
            if old.t_end > t:
                old.t_end, old.end_fade = t, HARD_STOP_FADE_S
            old = None
        if clip != prev_clip and cur is not None:
            if prev_vol > XFADE_LEVEL:
                ov, tt = prev_vol, t
                while True:
                    ov -= ov * k
                    done = ov < STOP_LEVEL
                    cur.gain_t.append(tt)
                    cur.gain_v.append(0.0 if done else ov)
                    if done:
                        break
                    tt += dt
                cur.t_end = tt
                cur.info["crossfade_at"] = t
                old = cur
            else:
                cur.t_end, cur.end_fade = t, HARD_STOP_FADE_S
            cur = None
        if clip and vol > 0 and cur is None and clip not in bad:
            try:
                length = _clip_seconds(clip, take.dir)
                off = float(rng.uniform(0.0, length)) if length > 1.0 else 0.0
                t0 = t - dt
                cur = Voice(layer, clip, True, t0, off, 1.0, [t0], [pitch], 1.0, [t0], [0.0], cut_t=[t0], cut_v=[cut],
                            kind="loop")
                voices.append(cur)
            except FileNotFoundError as e:
                bad.add(clip)
                warnings.append("%s: %s" % (layer, e))
        if cur is not None:
            cur.gain_t.append(t)
            cur.gain_v.append(vol)
            cur.rate_t.append(t)
            cur.rate_v.append(pitch)
            cur.cut_t.append(t)
            cur.cut_v.append(cut)
            if vol <= 0:
                cur.t_end = t
                cur = None
        prev_clip, prev_vol = clip, vol
    return voices


def _plan_machines(take, rng, lead, warm, master_of, warnings, trace=None):
    """One Machine per recorded tool, created the first Step it is current with `recorded` on (ToolAudio._recorded),
    then ticked every Step: a machine put down mid-run coasts while the next tool plays. `trace` (a list) receives
    {tool: (phase, speed)} after every Step (tests)."""
    machines, recs, first, mismatch = {}, {}, True, {}
    for o in take.steps:
        s = o["state"]
        tool = s.get("tool")
        if tool not in TOOLS:
            continue
        recorded, on_now, master = bool(s.get("recorded")), bool(s.get("machineOn")), master_of(s)
        if recorded and tool not in machines:
            if tool not in recs:
                recs[tool] = machine_recording(tool, take.dir, take.rec_names(tool))
                if recs[tool] is None:
                    warnings.append("%s: recorded, but no complete machine recording resolves" % tool)
            if recs[tool] is not None:
                machines[tool] = Machine(tool, lead)
                if warm and first and on_now:
                    e = entry(tool)
                    machines[tool].volume = master * float(e.get("motorVolume", 0.5))
                    machines[tool].warm_start(recs[tool], o["t"], float(e.get("motorPitch", 1.0)),
                                              float(rng.uniform(0.0, recs[tool].loop_len)))
        for name, m in machines.items():
            e = entry(name)
            current = name == tool
            load = 1.0 + float(e.get("motorSpeedPitch", 0.0)) * _f(s, "speed01") if current else 1.0
            m.tick(o["t"], current and recorded and on_now, recs[name] is not None, recs[name],
                   master * float(e.get("motorVolume", 0.5)), float(e.get("motorPitch", 1.0)), load, take.dt)
        if take.virtual_clock:
            _check_port(o, s.get("rec"), machines, mismatch)
        if trace is not None:
            trace.append({k: (m.phase, m.speed) for k, m in machines.items()})
        first = False
    for name, (n, t_first, what) in sorted(mismatch.items()):
        warnings.append("%s: the RecordedMachine port disagrees with the logged rec state at %d Steps (first at %.3f s: %s)"
                        % (name, n, t_first, what))
    return machines, recs


def _check_port(o, rec_state, machines, mismatch):
    """On a virtual-clock take the logged rec.<Tool>.phase / speed are the game's own: the port must match them Step
    for Step (speed within the F3 rounding)."""
    if not isinstance(rec_state, dict):
        return
    for name, lr in rec_state.items():
        if not isinstance(lr, dict) or "phase" not in lr:
            continue
        m = machines.get(name)
        phase, speed = (m.phase, m.speed) if m is not None else (OFF, 0.0)
        bad = None
        if lr["phase"] != phase:
            bad = "phase %s, port %s" % (lr["phase"], phase)
        elif abs(_f(lr, "speed") - speed) > 0.0015:
            bad = "speed %.3f, port %.4f" % (_f(lr, "speed"), speed)
        if bad:
            n, t_first, what = mismatch.get(name, (0, o["t"], bad))
            mismatch[name] = (n + 1, t_first, what)


def _slot_clip(tool, slot):
    """Name of a bank one-shot slot (powerOn / powerOff), else ToolAudio's runtime placeholder name."""
    name = clip_name(clip_for_guid(entry(tool).get(slot)))
    return name or "sfx_%s_%s" % (tool, "on" if slot == "powerOn" else "off")


def _plan_oneshots(take, latency, master_of, src_vol):
    """ToolAudio's PlayOneShot calls, re-derived from state changes the way StepFrame / OnToolChanged make them: a tool
    change plays the old machine's power-off (0.8, only when it was not a recording) and the switch tick (0.5); a
    powered non-recorded tool plays power-on when its machine starts and power-off when it stops (1.0). Each at the
    OneShot source's volume (`src_vol`) x its volumeScale, as Unity plays it: 0 in the game today (ONESHOT_SOURCE_VOLUME)."""
    voices, prev, ever_on = [], None, False
    switch = clip_name(clip_for_guid(bank().get("toolSwitchClick"))) or "sfx_tool_switch"

    def shot(name, scale, t, s, why):
        vol = src_vol * min(1.0, master_of(s) * scale)
        if vol > 0:
            voices.append(Voice("oneshot", name, False, t, gain=vol, kind="oneshot", info={"why": why}))

    for o in take.steps:
        s = o["state"]
        tool = s.get("tool")
        if tool not in TOOLS:
            continue
        t = o["t"] + latency
        on, rec, powered = bool(s.get("machineOn")), bool(s.get("recorded")), bool(s.get("powered"))
        if prev is None:
            prev, ever_on = (tool, on, rec), on
            continue
        ptool, was_on, prec = prev
        if tool != ptool:
            if was_on and not prec:
                shot(_slot_clip(ptool, "powerOff"), 0.8, t, s, "tool change, %s off" % ptool)
            shot(switch, 0.5, t, s, "tool change")
            was_on, ever_on = False, False
        if not rec and powered:
            if on and not was_on:
                shot(_slot_clip(tool, "powerOn"), 1.0, t, s, "power on")
                ever_on = True
            elif was_on and not on and ever_on:
                shot(_slot_clip(tool, "powerOff"), 1.0, t, s, "power off")
        elif on:
            ever_on = True
        prev = (tool, on, rec)
    return voices


def _plan_rattle(take, rng, master_of, scheduled=True, top_up=True):
    """Crumb strikes rolled from the state counters, lastLeadMs ahead, size / pitch / level rolled as OnDebrisSucked
    rolls them. `scheduled`: one per rattleScheduled increment (used when the log has no rattle events). `top_up`: the
    rattleDropped increments a Step could still have voiced (8 per Step minus what it scheduled). Only for a take on the
    FROZEN real clock, where every rattle voice looks busy after 14 strikes, so an unfocused take drops what the game
    would have played; a real over-the-cap drop (rattleMaxPerFrame = 8) comes with 8 scheduled in that Step and stays
    dropped. On a virtual-clock take every drop is the game's own, so build_plan passes top_up=False there."""
    voices, prev = [], None
    b = bank()
    for o in take.steps:
        s = o["state"]
        tool, n, nd = s.get("tool"), int(_f(s, "rattleScheduled")), int(_f(s, "rattleDropped"))
        if prev is None or tool not in TOOLS:
            prev = (n, nd)
            continue
        ds, dd = n - prev[0], nd - prev[1]
        prev = (n, nd)
        d = (max(0, ds) if scheduled else 0) + \
            (min(max(0, dd), max(0, RATTLE_MAX_PER_FRAME - max(0, ds))) if top_up else 0)
        if d <= 0:
            continue
        e = entry(tool)
        clips = [clip_name(clip_for_guid(g)) for g in e.get("debrisClips", []) if g] or \
            ["sfx_%s_debris_%d" % (tool, i) for i in range(8)]
        lead = _f(s, "lastLeadMs") / 1000.0
        lead = lead if lead > 0 else 0.15
        master = tool_audio_volume() * min(1.0, float(b.get("masterVolume", 0.85)))
        for j in range(min(d, RATTLE_MAX_PER_FRAME)):
            size01 = float(rng.random())
            half = len(clips) // 2
            pool = clips if len(clips) == 1 else (clips[half:] if size01 >= 4.0 / 9.0 else clips[:half])
            pitch = min(3.0, max(0.25, float(e.get("debrisPitch", 1)) * float(b.get("rattlePitch", 1)))) * \
                (1.12 + (0.88 - 1.12) * size01) * (0.96 + 0.08 * float(rng.random()))
            vol = min(1.0, master * float(e.get("debrisVolume", 0.55)) * float(b.get("rattleVolume", 1)) *
                      (0.45 + 0.55 * size01) * (0.8 + 0.2 * float(rng.random())))
            t = o["t"] + lead + 0.008 + 0.014 * float(rng.random()) + 0.004 * j
            voices.append(Voice("rattle", pool[int(rng.integers(len(pool)))], False, t, rate=pitch, gain=vol, kind="rattle"))
    return voices


_FAMILIES = {"tool": "ToolAudio", "loop": "ToolAudioLoop", "rec": "RecordedMachine", "ui": "UiAudio", "pre": "pre"}


def _family(src):
    """Who played an event. StoreGifApi's AudioTap names sources 'tool/powerOn|powerOff|switch|rattle', 'loop/MotorA',
    'rec/RecordedDryVacuum/start', 'ui/KaChing', 'ui/voice', 'pre/<GameObject>' (AUDIO.md 12); the lane contract said
    'ToolAudio', 'RecordedMachine', 'UiAudio', 'Debris', ... Both spellings map to one family."""
    src = str(src or "")
    if "/" in src:
        return _FAMILIES.get(src.split("/", 1)[0], src.split("/", 1)[0])
    return src


def _is_rattle(fam, src, kind, clip):
    return (kind == "scheduled" and fam in ("ToolAudio", "Debris")) or src.endswith("/rattle") or "debris" in clip.lower()


def _event_layer(src, kind, clip):
    fam = _family(src)
    if fam == "UiAudio":
        return "ui"
    if _is_rattle(fam, src, kind, clip):
        return "rattle"
    if fam == "ToolAudio":
        return "oneshot"
    return "ev_" + re.sub(r"[^a-z0-9]+", "", fam.lower() or "misc")


def _plan_events(take, latency, trust_machine, oneshot_src_vol=None, warnings=None):
    """Every logged event as a voice. `oneshot` / `play` start one latency after their call (the next mix block),
    `scheduled` exactly on time (`at` when logged, else the Step's time + `delay`). A `stop` cuts everything its source
    plays (one-shots too); a new `play` / `scheduled` on a source replaces what it plays (its one-shots ring on). Skipped:
    RecordedMachine events (the port above replays them), ToolAudio's loop play / stop (its loop layers come from the
    state), `playing` (a source already sounding when the take began: a loop the state covers, or a finished one-shot
    whose isPlaying the frozen Editor never cleared) and silent ones. `oneshot_src_vol` (None = the logged `vol`) plays
    ToolAudio's one-shots at that source volume x their `scale` instead (their OneShot source is at volume 0)."""
    voices, by_id, silenced = [], {}, 0
    for o in take.steps:
        for ev in o["ev"]:
            src, kind, clip = str(ev.get("src", "")), str(ev.get("kind", "oneshot")), str(ev.get("clip") or "")
            fam = _family(src)
            if fam == "RecordedMachine" and not trust_machine:
                continue
            if fam == "ToolAudioLoop" or (fam == "ToolAudio" and kind in ("play", "stop")) or kind == "playing":
                continue
            delay = float(ev.get("delay") or 0.0)
            at = ev.get("at")
            base = (float(at) - delay) if isinstance(at, (int, float)) else o["t"]    # the call's clock
            t_call = base + latency
            t = base + delay + (0.0 if kind == "scheduled" else latency)
            sid = ev.get("id")
            key = sid if sid not in (None, 0) else (src, None)    # the tap's ids are per source, whoever emits
            if kind == "stop" or (kind in ("play", "scheduled") and sid not in (None, 0)):
                cut = t if kind == "stop" else t_call
                for v in by_id.get(key, []):
                    if (kind == "stop" or v.kind in ("play", "scheduled")) and v.t_end > cut:
                        v.t_end, v.end_fade = cut, HARD_STOP_FADE_S
            if kind == "stop" or not clip:
                continue
            gain = float(ev.get("vol", 1.0) or 0.0)
            if fam == "ToolAudio" and kind == "oneshot" and not _is_rattle(fam, src, kind, clip):
                scale = float(ev.get("scale") or 0.0)
                if oneshot_src_vol is not None and "scale" in ev:
                    gain = min(1.0, scale) * float(oneshot_src_vol)
                elif gain <= 0 < scale:
                    silenced += 1
            if gain <= 0:
                continue
            v = Voice(_event_layer(src, kind, clip), clip, bool(ev.get("loop")) and kind != "oneshot", t,
                      rate=float(ev.get("pitch") or 1.0), gain=gain,
                      offset=max(0.0, float(ev.get("start") or 0.0)) if kind in ("play", "scheduled") else 0.0,
                      pan=float(ev.get("pan") or 0.0), id=sid, kind=kind, info={"src": src})
            voices.append(v)
            by_id.setdefault(key, []).append(v)
    if silenced and warnings is not None:
        warnings.append("%d ToolAudio one-shot(s) (power on / off, tool switch) logged vol 0: their OneShot source is "
                        "at volume 0, so the game plays them silent too (oneshot_source_volume=1 plays them)" % silenced)
    return voices


@dataclass
class Plan:
    take: Take
    sr: int
    voices: list
    warnings: list
    machines: dict
    opts: dict


_plans = {}


def build_plan(take_dir, sr=48000, seed=20260925, latency=None, lead_ms=None, warm=False, trust_machine_events=False,
               emulate_oneshots="auto", emulate_rattle="auto", oneshot_source_volume=None):
    """Everything the take plays, as Voices. `latency` (s, default one Step) = how late a per-frame change (volume,
    pitch, filter, a one-shot) is heard after its Step, as Unity picks it up at the next mix block and ramps over it.
    `lead_ms` overrides RecordedMachine's scheduling lead (default: the take's leadMs, else 42). `warm` starts a machine
    that is already on at the first Step in its loop. `seed` fixes the random loop entry points and rolled rattles.
    `emulate_*`: True (forced, even beside logged events) / False / "auto" (= only when the log carries no such
    events; for the rattle, "auto" beside logged events still tops up the strikes a FROZEN-clock take dropped). A
    virtual-clock take (header clock "virtual": AudioTap logs every one-shot and rattle strike the game makes) never
    emulates in "auto". `oneshot_source_volume`: None = ToolAudio's power / switch one-shots as the game plays them
    (its OneShot source is at volume 0: silent); 1.0 = as if that source were at full volume."""
    take = Take(take_dir)
    clips_json = os.path.join(take.dir, "audio_clips.json")
    key = (take.dir, os.path.getmtime(take.path), os.path.getmtime(clips_json) if os.path.isfile(clips_json) else 0,
           sr, seed, latency, lead_ms, warm, trust_machine_events, emulate_oneshots, emulate_rattle, oneshot_source_volume)
    if key in _plans:
        return _plans[key]
    rng = np.random.default_rng(seed)
    warnings = []
    lat = take.dt if latency is None else float(latency)
    lead = take.lead() if lead_ms is None else float(lead_ms) / 1000.0
    vol0 = tool_audio_volume() * min(1.0, float(bank().get("masterVolume", 0.85)))

    def master_of(s):
        return 0.0 if s.get("muted") else vol0

    if any(o["state"].get("muted") for o in take.steps):
        warnings.append("ToolAudio.muted was on during the take: its sound is silent there, as in the game")
    events = [ev for o in take.steps for ev in o["ev"]]
    fams = [(_family(ev.get("src")), str(ev.get("src", "")), str(ev.get("kind", "oneshot")), str(ev.get("clip") or ""))
            for ev in events]
    has_rattle = take.virtual_clock or any(fam in ("ToolAudio", "Debris") and _is_rattle(fam, src, kind, clip)
                                           for fam, src, kind, clip in fams)
    has_shots = take.virtual_clock or any(fam == "ToolAudio" and kind == "oneshot" and not _is_rattle(fam, src, kind, clip)
                                          for fam, src, kind, clip in fams)
    src_vol = ONESHOT_SOURCE_VOLUME if oneshot_source_volume is None else float(oneshot_source_volume)

    voices = []
    machines, _recs = _plan_machines(take, rng, lead, warm, master_of, warnings)
    for tool, m in machines.items():
        voices += m.to_voices("machine_" + tool)
    voices += _plan_loop_layer(take, "motor", "motor", rng, lat, warnings)
    voices += _plan_loop_layer(take, "contact", "contact", rng, lat, warnings)
    if emulate_oneshots is True or (emulate_oneshots == "auto" and not has_shots):
        voices += _plan_oneshots(take, lat, master_of, src_vol)
        if src_vol <= 0:
            n = len(_plan_oneshots(take, lat, master_of, 1.0))
            if n:
                warnings.append("%d re-derived ToolAudio one-shot(s) (power on / off, tool switch) left silent: the game's "
                                "OneShot source is at volume 0 (oneshot_source_volume=1 plays them)" % n)
    if emulate_rattle is True or (emulate_rattle == "auto" and not has_rattle):
        voices += _plan_rattle(take, rng, master_of, scheduled=True, top_up=not take.virtual_clock)
    elif emulate_rattle == "auto" and not take.virtual_clock:
        voices += _plan_rattle(take, rng, master_of, scheduled=False)   # only what the frozen clock dropped
    voices += _plan_events(take, lat, trust_machine_events, oneshot_source_volume, warnings)
    for v in voices:                                   # a clip that does not resolve plays nothing, loudly
        try:
            v.info["path"] = resolve_clip(v.clip, take.dir)
            if v.tail_clip:
                v.info["tail_path"] = resolve_clip(v.tail_clip, take.dir)
        except FileNotFoundError as e:
            warnings.append("%s: %s" % (v.layer, e))
            v.info["path"] = None
    credits = sorted({c for c in (clip_credit(v.clip) for v in voices if v.info.get("path")) if c})
    for c in credits:
        if re.search(r"\bNC\b|-NC", c):
            warnings.append("NON-COMMERCIAL recording in the mix, not for a trailer (TRAILER.md 7): " + c)
    plan = Plan(take, sr, [v for v in voices if v.info.get("path")], sorted(set(warnings)), machines,
                {"latency": lat, "lead": lead, "seed": seed, "master": vol0, "virtualClock": take.virtual_clock,
                 "oneshotSourceVolume": src_vol, "credits": credits})
    _plans[key] = plan
    return plan


# ------------------------------------------------------------------------------------------------ rendering
CHUNK = 1 << 18              # samples per render chunk (5.5 s at 48 kHz): bounds memory on long voices
LPF_BLOCK = 64               # samples per low-pass coefficient update (1.3 ms)
_sos_cache, _src_cache = {}, {}


def _knots(t, v):
    t, v = np.asarray(t, float), np.asarray(v, float)
    order = np.argsort(t, kind="stable")
    t, v = t[order], v[order]
    keep = np.append(t[1:] != t[:-1], True)          # of equal times the last one wins (a later write in the Step)
    return t[keep], v[keep]


def _cum(kt, kr, x):
    """Integral from kt[0] to x of the piecewise-linear rate through (kt, kr), held constant outside the knots."""
    x = np.asarray(x, float)
    if len(kt) == 1:
        return (x - kt[0]) * kr[0]
    c = np.concatenate([[0.0], np.cumsum(0.5 * (kr[1:] + kr[:-1]) * np.diff(kt))])
    i = np.clip(np.searchsorted(kt, x, side="right") - 1, 0, len(kt) - 1)
    dx = x - kt[i]
    nxt = np.minimum(i + 1, len(kt) - 1)
    slope = np.where(i < len(kt) - 1, (kr[nxt] - kr[i]) / np.maximum(kt[nxt] - kt[i], 1e-12), 0.0)
    val = c[i] + dx * (kr[i] + 0.5 * slope * dx)
    before = x < kt[0]
    return np.where(before, (x - kt[0]) * kr[0], val)


def _source(v, sr):
    """(clip rate, samples to read, loop). A looping clip is read at its own rate (the voice's rate absorbs the
    conversion, as FMOD does), so its seam stays seamless; a one-shot is resampled once, with a real filter, and padded
    with zeros (and the loop's head after a spin-up) so the interpolation never reads outside."""
    key = (v.info["path"], v.loop, v.info.get("tail_path"), sr)
    if key not in _src_cache:
        if v.loop:
            rate, x = load_native(v.info["path"])
            _src_cache[key] = (rate, x, len(x))
        else:
            x = load_wav(v.info["path"], sr)
            tail = load_wav(v.info["tail_path"], sr)[:4] if v.info.get("tail_path") else np.zeros((0, 2), np.float32)
            z = np.zeros((2, 2), np.float32)
            _src_cache[key] = (sr, np.concatenate([z, x, tail, np.zeros((4, 2), np.float32)]), len(x))
    return _src_cache[key]


def _interp(src, n_clip, pos, loop):
    """4-point Catmull-Rom read of `src` at fractional clip positions (exact at integer ones)."""
    i = np.floor(pos).astype(np.int64)
    f = (pos - i).astype(np.float32)
    if loop:
        idx = [np.mod(i + k, n_clip) for k in (-1, 0, 1, 2)]
    else:
        last = len(src) - 1                            # src = 2 zeros, clip, tail, 4 zeros: the ends read zero
        idx = [np.clip(i + k + 2, 0, last) for k in (-1, 0, 1, 2)]
    if not f.any():
        return src[idx[1]].copy()
    p0, p1, p2, p3 = (src[j] for j in idx)
    ff = f[:, None]
    return p1 + 0.5 * ff * (p2 - p0 + ff * (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3 + ff * (3.0 * (p1 - p2) + p3 - p0)))


def _lp_sos(fc, sr, q=LPF_RESONANCE_Q):
    """AudioLowPassFilter as two RBJ sections: FMOD's lowpass is 24 dB/oct, and resonance Q 1 is 'no bump'
    (ToolAudio.AddLowPass), so Q 1 = a 4th-order Butterworth; a higher Q raises the resonant section's."""
    fc = min(max(fc, 10.0), 0.45 * sr)
    key = (int(round(1200.0 * math.log2(fc))), sr, q)
    if key not in _sos_cache:
        w0 = 2.0 * math.pi * (2.0 ** (key[0] / 1200.0)) / sr
        cw, sw = math.cos(w0), math.sin(w0)
        secs = []
        for qq in (0.5411961, 1.3065630 * q):
            a = sw / (2.0 * qq)
            b0 = (1.0 - cw) / 2.0
            secs.append([b0 / (1 + a), 2 * b0 / (1 + a), b0 / (1 + a), 1.0, -2 * cw / (1 + a), (1 - a) / (1 + a)])
        _sos_cache[key] = np.array(secs)
    return _sos_cache[key]


def _tv_lowpass(y, n0, ck, sr, zi):
    """Time-varying low-pass over samples n0.. (absolute). Coefficient blocks sit on absolute multiples of LPF_BLOCK,
    each with the cutoff at its own centre, so a render that starts elsewhere computes the very same filter."""
    out = np.empty_like(y)
    b = 0
    while b < len(y):
        blk = (n0 + b) // LPF_BLOCK
        e = min(len(y), (blk + 1) * LPF_BLOCK - n0)
        fc = math.exp(float(np.interp((blk + 0.5) * LPF_BLOCK / sr, ck[0], ck[1])))
        out[b:e], zi = sosfilt(_lp_sos(fc, sr), y[b:e], axis=0, zi=zi)
        b = e
    return out, zi


def _gain(v, t, gk):
    g = np.interp(t, gk[0], gk[1]) if gk is not None else np.full(len(t), v.gain)
    if v.fade_in > 0:
        g = g * np.sin(0.5 * np.pi * np.clip((t - v.t_start) / v.fade_in, 0.0, 1.0))
    if math.isfinite(v.fade_out_at):
        x = np.clip((t - v.fade_out_at) / max(1e-3, v.fade_out), 0.0, 1.0)
        g = g * np.where(t > v.fade_out_at, np.cos(0.5 * np.pi * x), 1.0)
    if v.end_fade > 0 and math.isfinite(v.t_end):
        g = g * np.clip((v.t_end - t) / v.end_fade, 0.0, 1.0)
    return g.astype(np.float32)


def _render_voice(v, n_a, n_b, sr):
    """(first sample, float32 (n, 2)) of what `v` plays inside samples [n_a, n_b), or None."""
    rate_src, src, n_clip = _source(v, sr)
    if n_clip == 0:
        return None
    scale = rate_src / float(sr)
    ns = int(round(v.t_start * sr))                    # PlayScheduled lands on a sample; so does a Step's Play()
    ne = n_b if not math.isfinite(v.t_end) else min(n_b, int(round(v.t_end * sr)))
    if not v.loop and v.rate_t is None:                # a one-shot ends with its clip
        ne = min(ne, ns + int(math.ceil((n_clip + 2 - v.offset * rate_src) / max(1e-6, v.rate * scale))))
    lpf = v.cut_t is not None
    n_first = max(ns, n_a - (int(0.05 * sr) if lpf else 0))   # 50 ms of filter pre-roll
    if ne <= max(n_first, n_a):
        return None
    if v.rate_t is not None:
        kt, kr = _knots(np.asarray(v.rate_t) * sr, np.asarray(v.rate_v) * scale)
        base = _cum(kt, kr, [float(ns)])[0]
    gk = _knots(v.gain_t, v.gain_v) if v.gain_t is not None else None
    ck = _knots(v.cut_t, np.log(np.clip(np.asarray(v.cut_v, float), 10.0, OPEN_HZ))) if lpf else None
    zi = np.zeros((2, 2, 2)) if lpf else None
    out = np.empty((ne - n_first, 2), np.float32)
    for c0 in range(n_first, ne, CHUNK):
        c1 = min(ne, c0 + CHUNK)
        n = np.arange(c0, c1, dtype=np.float64)
        if v.rate_t is None:
            pos = v.offset * rate_src + (n - ns) * (v.rate * scale)
        else:
            pos = v.offset * rate_src + _cum(kt, kr, n) - base
        y = _interp(src, n_clip, pos, v.loop)
        if lpf:
            y, zi = _tv_lowpass(y, c0, ck, sr, zi)
        y *= _gain(v, n / sr, gk)[:, None]
        if v.pan:
            y[:, 0] *= min(1.0, 1.0 - v.pan)          # panStereo on a stereo clip acts as a balance
            y[:, 1] *= min(1.0, 1.0 + v.pan)
        out[c0 - n_first:c1 - n_first] = y
    k = max(0, n_a - n_first)
    return n_first + k, out[k:]


def render_take(take_dir, t0, t1, sr=48000, return_stems=False, **opts):
    """The take's sound from take time t0 to t1 (s) as float32 (n, 2) at `sr`, n = round(t1*sr) - round(t0*sr).
    Everything before t0 is simulated (machines, loop entry points, filter state), so a machine already running at t0
    sounds as it would there. `opts` go to build_plan. return_stems=True also returns {layer: float32 (n, 2)}."""
    plan = build_plan(take_dir, sr, **opts)
    n_a, n_b = int(round(t0 * sr)), int(round(t1 * sr))
    out = np.zeros((max(0, n_b - n_a), 2), np.float32)
    stems = {}
    for v in plan.voices:
        r = _render_voice(v, n_a, n_b, sr)
        if r is None:
            continue
        start, y = r
        i = start - n_a
        out[i:i + len(y)] += y
        if return_stems:
            stems.setdefault(v.layer, np.zeros_like(out))[i:i + len(y)] += y
    out[~np.isfinite(out)] = 0.0
    return (out, stems) if return_stems else out


def take_duration(take_dir):
    """Take time of the last logged Step."""
    return Take(take_dir).duration


def write_wav(path, x, sr=48000):
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    wavfile.write(path, sr, np.ascontiguousarray(x, dtype=np.float32))


def main(argv=None):
    ap = argparse.ArgumentParser(description="Render a StoreGifApi take's foley from its audio.jsonl.")
    ap.add_argument("take", help="take folder holding audio.jsonl (+ audio_clips.json)")
    ap.add_argument("out", help="output WAV (32-bit float, stereo)")
    ap.add_argument("--t0", type=float, default=0.0, help="take seconds (0 = SeqBegin)")
    ap.add_argument("--t1", type=float, default=None, help="take seconds; default = last Step + --tail")
    ap.add_argument("--tail", type=float, default=0.0, help="seconds after the last Step (coast-downs) when --t1 is not given")
    ap.add_argument("--stems", default=None, help="folder for one WAV per layer")
    ap.add_argument("--sr", type=int, default=48000)
    ap.add_argument("--seed", type=int, default=20260925)
    ap.add_argument("--warm", action="store_true", help="a machine already on at the first Step starts in its loop")
    ap.add_argument("--lead-ms", type=float, default=None)
    ap.add_argument("--latency-ms", type=float, default=None)
    ap.add_argument("--oneshot-volume", type=float, default=None,
                    help="ToolAudio's OneShot source volume for power / switch clicks (default: the game's, 0 = silent)")
    a = ap.parse_args(argv)
    opts = dict(seed=a.seed, warm=a.warm, lead_ms=a.lead_ms, oneshot_source_volume=a.oneshot_volume,
                latency=None if a.latency_ms is None else a.latency_ms / 1000.0)
    plan = build_plan(a.take, a.sr, **opts)
    t1 = a.t1 if a.t1 is not None else plan.take.duration + a.tail
    mix, stems = render_take(a.take, a.t0, t1, a.sr, return_stems=True, **opts)
    write_wav(a.out, mix, a.sr)
    if a.stems:
        for layer, y in stems.items():
            write_wav(os.path.join(a.stems, layer + ".wav"), y, a.sr)
    peak = float(np.max(np.abs(mix))) if len(mix) else 0.0
    rms = float(np.sqrt(np.mean(mix.astype(np.float64) ** 2))) if len(mix) else 0.0
    layers = {}
    for v in plan.voices:
        layers[v.layer] = layers.get(v.layer, 0) + 1
    print(json.dumps({"out": a.out, "t0": a.t0, "t1": round(t1, 4), "peakDb": round(20 * math.log10(peak + 1e-12), 2),
                      "rmsDb": round(20 * math.log10(rms + 1e-12), 2), "voices": layers,
                      "machines": {k: m.log for k, m in plan.machines.items()}, "credits": plan.opts["credits"],
                      "warnings": plan.warnings}, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
