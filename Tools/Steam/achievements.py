"""Refluff Steam achievements: checks and the Steamworks localization upload files (Docs/ACHIEVEMENTS.md).

Source of truth (never reorder: the order = AchievementId = Steamworks creation order = token index):
  SteamStore/achievements/source_en.json      10 achievements + 2 stats, English text, translator notes
  SteamStore/achievements/loc/<code>.json     one per game language except en (Tools/loc/loc_tool.py LANGS):
                                              {"_lang": code, "<API>": {"name", "desc"}, ...}
  SteamStore/achievements/icons/<API>.png + <API>_locked.png   256x256 icons (achieved / unachieved)
Outputs (build):
  SteamStore/achievements/upload/achievements_loc_all.vdf              Steamworks "All languages" localization file
  SteamStore/achievements/upload/single/achievements_loc_<steam>.vdf   one single-language file per language

Usage:
  python Tools/Steam/achievements.py check                   validate source, translations, Achievements.cs order, icons
  python Tools/Steam/achievements.py build [--tokens f.vdf]  check, then write the upload VDFs. Tokens default to
                                                             NEW_ACHIEVEMENT_3_<index>_NAME/_DESC; --tokens takes them from
                                                             a VDF downloaded from Steamworks (matched by English NAME)
  python Tools/Steam/achievements.py show [code]             API / name / desc table of one language (default en)
Exit code 1 on an error (warnings do not fail).
"""
import json
import os
import re
import struct
import sys
import unicodedata

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools", "loc"))
from loc_tool import LANGS, CJK  # noqa: E402  (constants only; the import has no side effects)

ACH_DIR = os.path.join(ROOT, "SteamStore", "achievements")
ACH_CS = os.path.join(ROOT, "Assets", "CarpetCleaning", "Scripts", "Steam", "Achievements.cs")
GAME_EN = os.path.join(ROOT, "Assets", "CarpetCleaning", "Localization", "Resources", "Loc", "en.json")

COUNT = 10                                   # achievements the game ships (AchievementId)
API_RE = re.compile(r"^ACH_[A-Z0-9_]+$")
STAT_RE = re.compile(r"^STAT_[A-Z0-9_]+$")
CS_ACH_RE = re.compile(r'"(ACH_[A-Z0-9_]+)"')
CS_STAT_RE = re.compile(r'"(STAT_[A-Z0-9_]+)"')
NUM_RE = re.compile(r"(?<![0-9])[0-9]+(?![0-9])")
NAME_MAX, DESC_MAX = 40, 100                 # soft limits (Steam's overlay / profile rows); half for CJK scripts
ICON_SIZE = 256
DEFAULT_TOKEN = "NEW_ACHIEVEMENT_3_{0}"      # Steamworks: stat block 3 (stats 1, 2 came first), creation index
STEAM_OF = {l[0]: l[3] for l in LANGS}
SCRIPT_OF = {l[0]: l[4] for l in LANGS}
CODE_OF_STEAM = {l[3]: l[0] for l in LANGS}


class Paths:
    """Where the data lives; the hidden --root / --cs options (or env ACH_ROOT / ACH_CS) point a test at a copy."""

    def __init__(self, root=None, cs=None):
        self.root = os.path.abspath(root or os.environ.get("ACH_ROOT") or ACH_DIR)
        self.cs = os.path.abspath(cs or os.environ.get("ACH_CS") or ACH_CS)
        self.source = os.path.join(self.root, "source_en.json")
        self.loc = os.path.join(self.root, "loc")
        self.icons = os.path.join(self.root, "icons")
        self.upload = os.path.join(self.root, "upload")
        self.single = os.path.join(self.upload, "single")

    def loc_file(self, code):
        return os.path.join(self.loc, code + ".json")

    def rel(self, path):
        for base in (ROOT, self.root):
            try:
                r = os.path.relpath(path, base)
                if not r.startswith(".."):
                    return r.replace("\\", "/")
            except ValueError:
                pass
        return path


class Report:
    def __init__(self):
        self.errors, self.warnings = [], []

    def err(self, msg):
        self.errors.append(msg)

    def warn(self, msg):
        self.warnings.append(msg)

    def print(self):
        for w in self.warnings:
            print("warn ", w)
        for e in self.errors:
            print("ERROR", e)
        print(f"{len(self.errors)} errors, {len(self.warnings)} warnings")


def _no_dup_pairs(pairs):
    d = {}
    for k, v in pairs:
        if k in d:
            raise ValueError(f"duplicate key {k!r}")
        d[k] = v
    return d


def read_json(path):
    """-> (object, raw text). Raises ValueError with a readable reason (BOM, bad JSON, duplicate keys)."""
    with open(path, "rb") as f:
        raw = f.read()
    if raw.startswith(b"\xef\xbb\xbf"):
        raise ValueError("starts with a UTF-8 BOM (write UTF-8 without BOM)")
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError as e:
        raise ValueError(f"not UTF-8: {e}")
    try:
        return json.loads(text, object_pairs_hook=_no_dup_pairs), text
    except json.JSONDecodeError as e:
        raise ValueError(f"bad JSON: {e}")


def canonical_layouts(obj):
    """The accepted layouts of a loc file: json.dump(indent=2), or one line per achievement (the translators' form)."""
    def dq(x):
        return json.dumps(x, ensure_ascii=False)
    rows = []
    for k, v in obj.items():
        if isinstance(v, dict):
            rows.append(f"  {dq(k)}: {{ " + ", ".join(f"{dq(f)}: {dq(x)}" for f, x in v.items()) + " }")
        else:
            rows.append(f"  {dq(k)}: {dq(v)}")
    return json.dumps(obj, ensure_ascii=False, indent=2) + "\n", "{\n" + ",\n".join(rows) + "\n}\n"


def visible_len(s):
    """Characters a reader sees: combining marks (Thai vowels / tones, Vietnamese accents) do not count."""
    return sum(1 for c in s if unicodedata.category(c) not in ("Mn", "Me", "Cf"))


def bad_chars(s):
    """Tabs, newlines and other control characters (a VDF value is one line)."""
    return sorted({repr(c) for c in s if unicodedata.category(c) in ("Cc", "Zl", "Zp")})


def non_ascii_digits(s):
    return list(dict.fromkeys(c for c in s if unicodedata.category(c) == "Nd" and not ("0" <= c <= "9")))


def has_number(s, n):
    return any(m == str(n) for m in NUM_RE.findall(s))


# --------------------------------------------------------------------------- source
def load_source(p, rep):
    """-> (achievements, stats) from source_en.json, validated; ([], []) when unreadable."""
    if not os.path.exists(p.source):
        rep.err(f"{p.rel(p.source)} missing")
        return [], []
    try:
        src, _ = read_json(p.source)
    except ValueError as e:
        rep.err(f"{p.rel(p.source)}: {e}")
        return [], []
    achs, stats = src.get("achievements"), src.get("stats")
    if not isinstance(achs, list) or not isinstance(stats, list):
        rep.err("source_en.json: 'achievements' and 'stats' must be lists")
        return [], []
    stat_by = {}
    for i, s in enumerate(stats):
        api = s.get("api", "")
        where = f"source stat #{i} {api!r}"
        if not STAT_RE.match(api or ""):
            rep.err(f"{where}: API name must match {STAT_RE.pattern}")
        if api in stat_by:
            rep.err(f"{where}: duplicate API name")
        if s.get("type") not in ("INT", "FLOAT", "AVGRATE"):
            rep.err(f"{where}: type must be INT, FLOAT or AVGRATE")
        lo, hi, d = s.get("min"), s.get("max"), s.get("default")
        if not all(isinstance(x, (int, float)) and not isinstance(x, bool) for x in (lo, hi, d)):
            rep.err(f"{where}: min, max and default must be numbers")
        elif not lo <= d <= hi or lo >= hi:
            rep.err(f"{where}: needs min <= default <= max and min < max (got {lo}, {d}, {hi})")
        if not s.get("display"):
            rep.warn(f"{where}: no display name")
        stat_by[api] = s
    apis = [a.get("api", "") for a in achs]
    if len(achs) != COUNT:
        rep.err(f"source has {len(achs)} achievements, the game ships {COUNT}")
    for i, a in enumerate(achs):
        api = a.get("api", "")
        where = f"source #{i} {api!r}"
        if not API_RE.match(api or ""):
            rep.err(f"{where}: API name must match {API_RE.pattern}")
        if apis.count(api) > 1:
            rep.err(f"{where}: duplicate API name")
        if not isinstance(a.get("hidden"), bool):
            rep.err(f"{where}: 'hidden' must be true or false")
        check_text("en", api, a.get("name"), a.get("desc"), rep, where)
        pr = a.get("progress")
        if pr is not None:
            st = stat_by.get(pr.get("stat") if isinstance(pr, dict) else None)
            if st is None:
                rep.err(f"{where}: progress stat {pr!r} is not a stat of the source")
            else:
                lo, hi = pr.get("min"), pr.get("max")
                if not isinstance(lo, int) or not isinstance(hi, int) or lo >= hi:
                    rep.err(f"{where}: progress needs integer min < max (got {lo}, {hi})")
                elif hi > st["max"] or lo < st["min"]:
                    rep.err(f"{where}: progress {lo}..{hi} outside {st['api']} range {st['min']}..{st['max']}")
                elif not has_number(a.get("desc") or "", hi):
                    rep.err(f"{where}: English desc does not say the progress target {hi}")
    check_terms(achs, rep)
    return achs, stats


def check_terms(achs, rep):
    """'terms' must be keys of the game's en.json (the translator reuses their translations)."""
    if not os.path.exists(GAME_EN):
        return
    try:
        keys = set(read_json(GAME_EN)[0])
    except ValueError:
        return
    for a in achs:
        for t in a.get("terms") or []:
            if t not in keys:
                rep.warn(f"source {a.get('api')!r}: term {t!r} is not a key of the game's Loc/en.json")


def required_numbers(a):
    """Numbers a translation must keep as ASCII digits: the progress target (25 carpets, 10 tools) = error if lost;
    other numbers of the English desc (100 %) = warning if lost."""
    must = {a["progress"]["max"]} if isinstance(a.get("progress"), dict) and "max" in a["progress"] else set()
    other = {int(n) for n in NUM_RE.findall(a.get("desc") or "")} - must
    return must, other


def check_text(code, api, name, desc, rep, where):
    cjk = SCRIPT_OF.get(code) in CJK
    for field, text, limit in (("name", name, NAME_MAX), ("desc", desc, DESC_MAX)):
        if not isinstance(text, str) or not text.strip():
            rep.err(f"{where}: empty {field}")
            continue
        bad = bad_chars(text)
        if bad:
            rep.err(f"{where}: {field} has tab / newline / control characters {', '.join(bad)}")
        if text != text.strip():
            rep.warn(f"{where}: {field} has leading / trailing spaces")
        if "  " in text:
            rep.warn(f"{where}: {field} has a double space")
        digits = non_ascii_digits(text)
        if digits:
            rep.err(f"{where}: {field} uses non-ASCII digits {''.join(digits)!r}: write numbers with 0-9")
        lim = limit // 2 if cjk else limit
        n = visible_len(text)
        if n > lim:
            rep.warn(f"{where}: {field} is {n} characters (soft limit {lim}{' for CJK' if cjk else ''}): {text!r}")


# --------------------------------------------------------------------------- Achievements.cs
def check_cs(p, achs, stats, rep):
    if not os.path.exists(p.cs):
        rep.warn(f"{p.rel(p.cs)} missing: API order / stat names not cross-checked")
        return
    with open(p.cs, "r", encoding="utf-8-sig") as f:
        text = f.read()
    def ordered(rx):
        seen = []
        for m in rx.findall(text):
            if m not in seen:
                seen.append(m)
        return seen
    cs_achs, cs_stats = ordered(CS_ACH_RE), ordered(CS_STAT_RE)
    src_achs, src_stats = [a.get("api") for a in achs], [s.get("api") for s in stats]
    if cs_achs != src_achs:
        for i in range(max(len(cs_achs), len(src_achs))):
            c = cs_achs[i] if i < len(cs_achs) else None
            s = src_achs[i] if i < len(src_achs) else None
            if c is None:
                rep.err(f"Achievements.cs has no API #{i} (source_en.json: {s!r})")
            elif s is None:
                rep.err(f"Achievements.cs API #{i} {c!r} is not in source_en.json")
            elif c != s:
                rep.err(f"Achievements.cs API #{i} is {c!r}, source_en.json has {s!r} (same order = AchievementId)")
    if set(cs_stats) != set(src_stats):
        rep.err(f"Achievements.cs stats {cs_stats} != source_en.json stats {src_stats}")


# --------------------------------------------------------------------------- translations
def load_loc(p, code, achs, rep):
    """-> {api: (name, desc)} of one language (validated), or None when the file is missing / unreadable."""
    path = p.loc_file(code)
    fn = f"loc/{code}.json"
    if not os.path.exists(path):
        rep.err(f"{fn} missing")
        return None
    try:
        data, text = read_json(path)
    except ValueError as e:
        rep.err(f"{fn}: {e}")
        return None
    if not isinstance(data, dict):
        rep.err(f"{fn}: must be a JSON object")
        return None
    if data.get("_lang") != code:
        rep.err(f"{fn}: '_lang' is {data.get('_lang')!r}, expected {code!r}")
    apis = [a["api"] for a in achs]
    for k in data:
        if k != "_lang" and k not in apis:
            rep.err(f"{fn}: unknown key {k!r}")
    out = {}
    for a in achs:
        api, where = a["api"], f"{fn} {a['api']}"
        e = data.get(api)
        if e is None:
            rep.err(f"{where}: missing")
            continue
        if not isinstance(e, dict):
            rep.err(f"{where}: must be an object {{\"name\", \"desc\"}}")
            continue
        for k in e:
            if k not in ("name", "desc"):
                rep.err(f"{where}: unknown key {k!r}")
        name, desc = e.get("name"), e.get("desc")
        check_text(code, api, name, desc, rep, where)
        if not isinstance(name, str) or not isinstance(desc, str):
            continue
        if name.strip() == a["name"]:
            rep.warn(f"{where}: name is the English text {name!r}")
        if desc.strip() == a["desc"]:
            rep.warn(f"{where}: desc is the English text {desc!r}")
        must, other = required_numbers(a)
        for n in sorted(must):
            if not has_number(desc, n):
                rep.err(f"{where}: desc must contain the number {n} as digits: {desc!r}")
        for n in sorted(other):
            if not has_number(desc, n):
                rep.warn(f"{where}: desc lost the number {n} of the English text: {desc!r}")
        out[api] = (name.strip(), desc.strip())
    order = [k for k in data if k in apis]
    if order != [a for a in apis if a in data]:
        rep.warn(f"{fn}: achievements are not in source order")
    if text not in canonical_layouts(data):
        rep.warn(f"{fn}: layout is neither json indent=2 nor one line per achievement (2 spaces, LF, final newline)")
    return out


def all_texts(p, achs, rep):
    """-> {code: {api: (name, desc)}} for en + every language whose file loaded (errors go to rep)."""
    texts = {"en": {a["api"]: (a["name"], a["desc"]) for a in achs}}
    for code in [l[0] for l in LANGS if l[0] != "en"]:
        t = load_loc(p, code, achs, rep)
        if t is not None:
            texts[code] = t
    return texts


# --------------------------------------------------------------------------- icons
def png_size(path):
    with open(path, "rb") as f:
        head = f.read(24)
    if len(head) < 24 or head[:8] != b"\x89PNG\r\n\x1a\n" or head[12:16] != b"IHDR":
        return None
    return struct.unpack(">II", head[16:24])


def check_icons(p, achs, rep):
    if not os.path.isdir(p.icons):
        rep.warn(f"{p.rel(p.icons)}/ missing: no icons checked")
        return
    for a in achs:
        for suffix in ("", "_locked"):
            fn = f"icons/{a['api']}{suffix}.png"
            path = os.path.join(p.icons, a["api"] + suffix + ".png")
            if not os.path.exists(path):
                rep.warn(f"{fn} missing")
                continue
            size = png_size(path)
            if size is None:
                rep.err(f"{fn}: not a PNG")
            elif size != (ICON_SIZE, ICON_SIZE):
                rep.err(f"{fn}: {size[0]}x{size[1]}, Steamworks wants {ICON_SIZE}x{ICON_SIZE}")


# --------------------------------------------------------------------------- check
def run_check(p):
    """-> (report, achievements, stats, texts)."""
    rep = Report()
    achs, stats = load_source(p, rep)
    if not achs or any(not API_RE.match(a.get("api") or "") for a in achs):
        return rep, achs, stats, {}
    check_cs(p, achs, stats, rep)
    texts = all_texts(p, achs, rep)
    check_icons(p, achs, rep)
    return rep, achs, stats, texts


def cmd_check(p):
    rep, _, _, texts = run_check(p)
    if texts:
        print(f"{p.rel(p.source)}: {len(texts)} of {len(LANGS)} languages loaded")
    else:
        print(f"{p.rel(p.source)}: source errors, translations / Achievements.cs / icons not checked")
    rep.print()
    return 1 if rep.errors else 0


# --------------------------------------------------------------------------- VDF
def vq(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def token_lines(achs, texts_of, tokens, indent):
    out = []
    for a in achs:
        name, desc = texts_of[a["api"]]
        out.append(f"{indent}{vq(tokens[a['api']] + '_NAME')}\t{vq(name)}")
        out.append(f"{indent}{vq(tokens[a['api']] + '_DESC')}\t{vq(desc)}")
    return out


def vdf_all(achs, texts, tokens):
    lines = ['"lang"', "{"]
    for code, _, _, steam, _ in LANGS:
        lines += [f"\t{vq(steam)}", "\t{", '\t\t"Tokens"', "\t\t{"]
        lines += token_lines(achs, texts[code], tokens, "\t\t\t")
        lines += ["\t\t}", "\t}"]
    return "\n".join(lines + ["}"]) + "\n"


def vdf_single(achs, texts, tokens, code):
    lines = ['"lang"', "{", f'\t"Language"\t{vq(STEAM_OF[code])}', '\t"Tokens"', "\t{"]
    lines += token_lines(achs, texts[code], tokens, "\t\t")
    return "\n".join(lines + ["\t}", "}"]) + "\n"


def parse_vdf(text):
    """KeyValues text -> [(key, str | [(key, ...)])]. Handles quotes, escapes, // comments and [$COND] tags."""
    toks, i, n = [], 0, len(text)
    while i < n:
        c = text[i]
        if c.isspace():
            i += 1
        elif text.startswith("//", i):
            j = text.find("\n", i)
            i = n if j < 0 else j
        elif c in "{}":
            toks.append(c)
            i += 1
        elif c == '"':
            buf, i = [], i + 1
            while i < n and text[i] != '"':
                if text[i] == "\\" and i + 1 < n:
                    buf.append({"n": "\n", "t": "\t", "\\": "\\", '"': '"'}.get(text[i + 1], "\\" + text[i + 1]))
                    i += 2
                else:
                    buf.append(text[i])
                    i += 1
            if i >= n:
                raise ValueError("unterminated string")
            toks.append(("s", "".join(buf)))
            i += 1
        else:
            j = i
            while j < n and not text[j].isspace() and text[j] not in '{}"':
                j += 1
            word = text[i:j]
            if not (word.startswith("[") and word.endswith("]")):
                toks.append(("s", word))
            i = j
    pos = 0

    def block(top):
        nonlocal pos
        pairs = []
        while pos < len(toks):
            t = toks[pos]
            if t == "}":
                if top:
                    raise ValueError("unbalanced '}'")
                pos += 1
                return pairs
            if t == "{":
                raise ValueError("'{' without a key")
            key, pos = t[1], pos + 1
            if pos >= len(toks):
                raise ValueError(f"key {key!r} has no value")
            if toks[pos] == "{":
                pos += 1
                pairs.append((key, block(False)))
            elif toks[pos] == "}":
                raise ValueError(f"key {key!r} has no value")
            else:
                pairs.append((key, toks[pos][1]))
                pos += 1
        if not top:
            raise ValueError("missing '}'")
        return pairs

    return block(True)


def read_text_any(path):
    with open(path, "rb") as f:
        raw = f.read()
    if raw[:2] in (b"\xff\xfe", b"\xfe\xff"):
        return raw.decode("utf-16")
    return raw.decode("utf-8-sig")


def vdf_tokens(tree):
    """-> {steam language or None: [(token, text)]}: 'lang' {Language X Tokens {}} or 'lang' {X {Tokens {}}}."""
    found = {}

    def walk(pairs, lang):
        named = next((v.lower() for k, v in pairs if k.lower() == "language" and isinstance(v, str)), None)
        for k, v in pairs:
            if not isinstance(v, list):
                continue
            if k.lower() == "tokens":
                found.setdefault(named or lang, []).extend((kk, vv) for kk, vv in v if isinstance(vv, str))
            else:
                walk(v, k.lower() if k.lower() in CODE_OF_STEAM else lang)

    walk(tree, None)
    return found


def tokens_from_file(path, achs, rep):
    """-> {api: token prefix} from a VDF downloaded from Steamworks, matched by the English NAME."""
    try:
        found = vdf_tokens(parse_vdf(read_text_any(path)))
    except (OSError, UnicodeDecodeError, ValueError) as e:
        rep.err(f"--tokens {path}: {e}")
        return None
    pairs = found.get("english")
    if pairs is None and list(found) == [None]:
        pairs = found[None]
        rep.warn(f"--tokens {path}: no language named, read as English")
    if not pairs:
        rep.err(f"--tokens {path}: no English tokens (download the English or the all-languages file); "
                f"found {sorted(str(k) for k in found)}")
        return None
    texts = dict(pairs)
    by_name = {}
    for a in achs:
        by_name.setdefault(a["name"].casefold(), a["api"])
    tokens, unmatched = {}, []
    for tok, text in pairs:
        if not tok.upper().endswith("_NAME"):
            continue
        api = by_name.get(text.strip().casefold())
        if api is None:
            unmatched.append(f"{tok}={text!r}")
            continue
        prefix = tok[:-5]
        if api in tokens and tokens[api] != prefix:
            rep.err(f"--tokens: {api} ({text!r}) matches both {tokens[api]} and {prefix}")
            continue
        tokens[api] = prefix
    for u in unmatched:
        rep.warn(f"--tokens: {u} matches no achievement name (ignored)")
    by_api = {a["api"]: a for a in achs}
    for api, prefix in tokens.items():
        desc = texts.get(prefix + "_DESC")
        if desc is None:
            rep.warn(f"--tokens: {prefix}_DESC ({api}) not in the file")
        elif desc.strip() != by_api[api]["desc"]:
            rep.warn(f"--tokens: {api} English desc in Steamworks {desc!r} != source {by_api[api]['desc']!r} "
                     f"(the upload replaces it)")
    for a in achs:
        if a["api"] not in tokens:
            rep.err(f"--tokens: no token has the English name of {a['api']} ({a['name']!r})")
    return tokens if len(tokens) == len(achs) else None


def write_text(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def cmd_build(p, tokens_path):
    rep, achs, _, texts = run_check(p)
    tokens = {a["api"]: DEFAULT_TOKEN.format(i) for i, a in enumerate(achs)}
    if tokens_path and not rep.errors:
        tokens = tokens_from_file(tokens_path, achs, rep) or tokens
    missing = [l[0] for l in LANGS if l[0] not in texts or len(texts[l[0]]) != len(achs)]
    if missing and not rep.errors:
        rep.err(f"incomplete languages: {missing}")
    if rep.errors:
        rep.print()
        print("build: nothing written (fix the errors above)")
        return 1
    for w in rep.warnings:
        print("warn ", w)
    out = os.path.join(p.upload, "achievements_loc_all.vdf")
    write_text(out, vdf_all(achs, texts, tokens))
    print(f"wrote {p.rel(out)}  ({len(LANGS)} languages x {len(achs)} achievements)")
    for code, _, _, steam, _ in LANGS:
        write_text(os.path.join(p.single, f"achievements_loc_{steam}.vdf"), vdf_single(achs, texts, tokens, code))
    print(f"wrote {p.rel(p.single)}/achievements_loc_<steam language>.vdf x {len(LANGS)}")
    src = "--tokens " + os.path.basename(tokens_path) if tokens_path else "default NEW_ACHIEVEMENT_3_<index>"
    print(f"tokens ({src}):")
    for i, a in enumerate(achs):
        print(f"  #{i:<2} {a['api']:<18} {tokens[a['api']]}_NAME / _DESC")
    print(f"0 errors, {len(rep.warnings)} warnings")
    return 0


# --------------------------------------------------------------------------- show
def width(s):
    return sum(0 if unicodedata.category(c) in ("Mn", "Me", "Cf") else
               2 if unicodedata.east_asian_width(c) in ("W", "F") else 1 for c in s)


def pad(s, w):
    return s + " " * max(0, w - width(s))


def cmd_show(p, lang):
    code = CODE_OF_STEAM.get(lang.lower(), lang)
    if code not in STEAM_OF:
        print(f"unknown language {lang!r}; codes: {' '.join(STEAM_OF)}")
        return 2
    rep = Report()
    achs, stats = load_source(p, rep)
    if not achs:
        rep.print()
        return 1
    rows = {a["api"]: (a["name"], a["desc"]) for a in achs} if code == "en" else load_loc(p, code, achs, rep)
    if rows is None or len(rows) != len(achs):
        rep.print()
        return 1
    print(f"{code} / {STEAM_OF[code]}")
    wa = max(len(a["api"]) for a in achs)
    wn = max(width(rows[a["api"]][0]) for a in achs)
    print(f"{'#':<3}{pad('API', wa)}  {pad('Name', wn)}  Description")
    for i, a in enumerate(achs):
        name, desc = rows[a["api"]]
        print(f"{i:<3}{pad(a['api'], wa)}  {pad(name, wn)}  {desc}")
    if code == "en":
        print("\nStats (create first; API / type / display / min..max / default / increment only / set by):")
        for s in stats:
            print(f"   {s['api']:<{wa}}  {s['type']:<5} {s['display']!r}  {s['min']}..{s['max']}  default {s['default']}"
                  f"  incrementOnly={s.get('incrementOnly')}  setBy={s.get('setBy')}")
        print("\nAchievement settings (hidden / progress stat min..max):")
        for i, a in enumerate(achs):
            pr = a.get("progress")
            prs = f"{pr['stat']} {pr['min']}..{pr['max']}" if pr else "-"
            print(f"{i:<3}{a['api']:<{wa}}  hidden={a['hidden']}  progress={prs}")
    if rep.errors or rep.warnings:
        print()
        rep.print()
    return 1 if rep.errors else 0


# --------------------------------------------------------------------------- main
def main(argv):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    args, opts = [], {}
    it = iter(argv[1:])
    for a in it:
        if a in ("--root", "--cs", "--tokens"):
            v = next(it, None)
            if v is None:
                print(f"{a} needs a value")
                return 2
            opts[a[2:]] = v
        elif a in ("-h", "--help"):
            print(__doc__)
            return 0
        else:
            args.append(a)
    if not args:
        print(__doc__)
        return 0
    p = Paths(opts.get("root"), opts.get("cs"))
    cmd = args[0]
    if cmd == "check" and len(args) == 1:
        return cmd_check(p)
    if cmd == "build" and len(args) == 1:
        return cmd_build(p, opts.get("tokens"))
    if cmd == "show" and len(args) <= 2:
        return cmd_show(p, args[1] if len(args) > 1 else "en")
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
