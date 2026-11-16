"""Localization tool (Docs/LOCALIZATION.md).

  python Tools/loc/loc_tool.py sync            Tools/loc/strings.json -> Resources/Loc/en.json (the key list the game ships)
  python Tools/loc/loc_tool.py check [codes]   every language file: keys, placeholders, casing, lengths, kept names,
                                               glyph coverage of its font; plus every UI string in UXML / C# / Credits.txt
                                               must be a key. Exit code 1 on an error (warnings do not fail).
  python Tools/loc/loc_tool.py langs           the language table (code, Steam API name, script, file present)

Keys are the English text exactly as the game shows it; en.json maps each key to itself. A language file maps every key
to its translation and nothing else. Placeholders are {0}, {1} (string.Format, invariant culture).
"""
import json
import os
import re
import sys
import unicodedata

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
STRINGS = os.path.join(ROOT, "Tools", "loc", "strings.json")
LOC_DIR = os.path.join(ROOT, "Assets", "CarpetCleaning", "Localization", "Resources", "Loc")
UXML = os.path.join(ROOT, "Assets", "CarpetCleaning", "UI", "CarpetHud.uxml")
CREDITS = os.path.join(ROOT, "Assets", "CarpetCleaning", "UI", "Credits.txt")
SCRIPTS = os.path.join(ROOT, "Assets", "CarpetCleaning", "Scripts")
FONT_DIR = os.path.join(ROOT, "Assets", "CarpetCleaning", "UI", "Fonts")
RP_DIR = os.path.join(ROOT, "Tools", "Steam")
INTER = os.path.join(ROOT, "Tools", "loc", "fonts_src", "Inter-VF.ttf")   # coverage check only (stands in for the built-in Inter), never shipped

# code, English name, native name, Steam API language, script (= font family, see Loc.cs / LOCALIZATION.md 4).
# Order = the drop-down order = Steamworks' own list (the big five, then alphabetical by English name).
LANGS = [
    ("en", "English", "English", "english", "base"),
    ("fr", "French", "Français", "french", "base"),
    ("it", "Italian", "Italiano", "italian", "base"),
    ("de", "German", "Deutsch", "german", "base"),
    ("es", "Spanish (Spain)", "Español (España)", "spanish", "base"),
    ("ar", "Arabic", "العربية", "arabic", "arabic"),
    ("bg", "Bulgarian", "Български", "bulgarian", "base"),
    ("cs", "Czech", "Čeština", "czech", "base"),
    ("da", "Danish", "Dansk", "danish", "base"),
    ("nl", "Dutch", "Nederlands", "dutch", "base"),
    ("fi", "Finnish", "Suomi", "finnish", "base"),
    ("el", "Greek", "Ελληνικά", "greek", "base"),
    ("hu", "Hungarian", "Magyar", "hungarian", "base"),
    ("id", "Indonesian", "Bahasa Indonesia", "indonesian", "base"),
    ("ja", "Japanese", "日本語", "japanese", "ja"),
    ("ko", "Korean", "한국어", "koreana", "ko"),
    ("ms", "Malay", "Bahasa Melayu", "malay", "base"),
    ("no", "Norwegian", "Norsk", "norwegian", "base"),
    ("pl", "Polish", "Polski", "polish", "base"),
    ("pt-BR", "Portuguese (Brazil)", "Português (Brasil)", "brazilian", "base"),
    ("pt-PT", "Portuguese (Portugal)", "Português (Portugal)", "portuguese", "base"),
    ("ro", "Romanian", "Română", "romanian", "base"),
    ("ru", "Russian", "Русский", "russian", "base"),
    ("zh-Hans", "Simplified Chinese", "简体中文", "schinese", "sc"),
    ("es-419", "Spanish (Latin America)", "Español (Latinoamérica)", "latam", "base"),
    ("sv", "Swedish", "Svenska", "swedish", "base"),
    ("th", "Thai", "ไทย", "thai", "thai"),
    ("zh-Hant", "Traditional Chinese", "繁體中文", "tchinese", "tc"),
    ("tr", "Turkish", "Türkçe", "turkish", "base"),
    ("uk", "Ukrainian", "Українська", "ukrainian", "base"),
    ("vi", "Vietnamese", "Tiếng Việt", "vietnamese", "base"),
]
# script -> the bundled font that draws it (Latin, Greek and Cyrillic use Unity's built-in Inter)
SCRIPT_FONT = {"arabic": "NotoSansArabic", "thai": "NotoSansThai", "ja": "NotoSansJP", "ko": "NotoSansKR",
               "sc": "NotoSansSC", "tc": "NotoSansTC"}
CJK = ("ja", "ko", "sc", "tc")
CASED_SCRIPTS = ("base",)   # scripts with upper/lower case (Latin, Greek, Cyrillic: all drawn by Inter = "base")
PLACEHOLDER = re.compile(r"\{\d+\}")
KEEP_TOKENS = ["Vahov Games", "Freesound.org", "Mixkit (mixkit.co)", "kyles", "richwise", "RossJuterbock", "crcavol",
               "yehdawgo", "CC0", "Tripo AI (tripo3d.ai)", "Unity", "THIRD_PARTY_NOTICES.txt"]
RP_TOKENS = ["#Status_Cleaning", "#Status_Spotless", "#Status_BetweenCarpets"]


def load_json(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def master():
    data = load_json(STRINGS)
    return data["strings"], data["rich_presence"]


def lang_path(code):
    return os.path.join(LOC_DIR, code + ".json")


def write_json(path, obj):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
        f.write("\n")


def cmd_sync():
    strings, _ = master()
    keys = [s["key"] for s in strings]
    dup = sorted({k for k in keys if keys.count(k) > 1})
    if dup:
        print("ERROR duplicate keys:", dup)
        return 1
    write_json(lang_path("en"), {k: k for k in keys})
    print("wrote", os.path.relpath(lang_path("en"), ROOT), len(keys), "keys")
    return 0


def is_upper_key(key):
    letters = [c for c in PLACEHOLDER.sub("", key) if c.isalpha()]
    return len(letters) > 0 and all(c.isupper() for c in letters)


def visible_len(s):
    return len(PLACEHOLDER.sub("0", s))


# --------------------------------------------------------------------------- source scan
def uxml_texts(named=False):
    """Static texts of UNNAMED text elements, like HudLocalizer registers them; named=True: the texts of NAMED ones instead
    (driven from C#; ShopView reads its item blurbs from there as keys)."""
    out = []
    src = open(UXML, "r", encoding="utf-8").read()
    for m in re.finditer(r"<ui:(Label|Button)\b([^>]*)>", src):
        attrs = m.group(2)
        t = re.search(r'\btext="([^"]*)"', attrs)
        if not t or not t.group(1).strip():
            continue
        if bool(re.search(r'\bname="', attrs)) != named:
            continue
        out.append(t.group(1).replace("&amp;", "&").replace("&quot;", '"').replace("&lt;", "<").replace("&gt;", ">"))
    return out


def code_literals():
    """String literals passed to Loc.T / Loc.F / Loc.Key(...) in C#."""
    out = []
    for dirpath, _, files in os.walk(SCRIPTS):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            src = open(os.path.join(dirpath, fn), "r", encoding="utf-8").read()
            for m in re.finditer(r'\bLoc\.(?:T|F|Key)\(\s*"((?:[^"\\]|\\.)*)"', src):
                out.append((fn, m.group(1).encode().decode("unicode_escape").encode("latin-1").decode("utf-8")))
    return out


def credits_lines():
    out = []
    for raw in open(CREDITS, "r", encoding="utf-8").read().replace("\r", "").split("\n"):
        line = raw.strip()
        if not line:
            continue
        out.append(line[2:].upper() if line.startswith("# ") else line)
    return out


# --------------------------------------------------------------------------- glyph coverage
_cmaps = {}


def font_cmap(path):
    if path in _cmaps:
        return _cmaps[path]
    try:
        from fontTools.ttLib import TTFont
    except ImportError:
        _cmaps[path] = None
        return None
    if not os.path.exists(path):
        _cmaps[path] = None
        return None
    f = TTFont(path, lazy=True)
    _cmaps[path] = set(f.getBestCmap().keys())
    return _cmaps[path]


def uncovered(text, script):
    """Characters of `text` that the font chain of `script` cannot draw (None = the check could not run)."""
    chain = []
    if script in SCRIPT_FONT:
        chain.append(os.path.join(FONT_DIR, SCRIPT_FONT[script] + "-Regular.ttf"))
    chain.append(INTER)
    cmaps = [font_cmap(p) for p in chain]
    if any(c is None for c in cmaps):
        return None
    miss = set()
    for ch in text:
        cp = ord(ch)
        if ch.isspace() or unicodedata.category(ch) in ("Cc", "Cf"):
            continue
        if not any(cp in c for c in cmaps):
            miss.add(ch)
    return miss


# --------------------------------------------------------------------------- checks
def check_lang(code, script, strings, errors, warnings):
    path = lang_path(code)
    if not os.path.exists(path):
        errors.append(f"{code}: file missing ({os.path.relpath(path, ROOT)})")
        return
    try:
        table = load_json(path)
    except Exception as e:
        errors.append(f"{code}: invalid JSON: {e}")
        return
    keys = [s["key"] for s in strings]
    for k in keys:
        if k not in table:
            errors.append(f"{code}: missing key {k!r}")
    for k in table:
        if k not in keys:
            errors.append(f"{code}: unknown key {k!r}")
    if list(table.keys()) != [k for k in keys if k in table]:
        warnings.append(f"{code}: keys are not in strings.json order")
    all_text = []
    for s in strings:
        k = s["key"]
        if k not in table:
            continue
        v = table[k]
        if not isinstance(v, str) or not v.strip():
            errors.append(f"{code}: empty value for {k!r}")
            continue
        all_text.append(v)
        if v != v.strip():
            warnings.append(f"{code}: leading/trailing space in {k!r}: {v!r}")
        if sorted(PLACEHOLDER.findall(k)) != sorted(PLACEHOLDER.findall(v)):
            errors.append(f"{code}: placeholders differ in {k!r}: {v!r}")
        for tok in KEEP_TOKENS:
            if tok in k and tok not in v:
                errors.append(f"{code}: {tok!r} must stay unchanged in {k!r}: {v!r}")
        if code != "en" and script in CASED_SCRIPTS and is_upper_key(k):
            letters = [c for c in PLACEHOLDER.sub("", v) if c.isalpha()]
            if letters and not all(c.isupper() or not c.islower() for c in letters):
                warnings.append(f"{code}: caption should be uppercase: {k!r} -> {v!r}")
        mx = s.get("max", 0)
        if mx:
            limit = mx * (0.75 if script in CJK else 1.0)
            n = visible_len(v)
            if n > limit * 1.25:
                warnings.append(f"{code}: long ({n} > {int(limit)}) {k!r} -> {v!r}")
    # POWER / AREA legends must start with the stat names
    for stat, legend in (("POWER", "POWER works faster"), ("AREA", "AREA covers more per stroke")):
        if stat in table and legend in table and not table[legend].startswith(table[stat]):
            warnings.append(f"{code}: {legend!r} should begin with {table[stat]!r}: {table[legend]!r}")
    miss = uncovered("".join(all_text), script)
    if miss is None:
        warnings.append(f"{code}: glyph coverage not checked (fonts or fontTools missing)")
    elif miss:
        errors.append(f"{code}: characters without a glyph in its font chain: {''.join(sorted(miss))!r}")


def check_rich_presence(errors, warnings):
    for code, _, _, steam, _ in LANGS:
        path = os.path.join(RP_DIR, f"rich_presence_{steam}.vdf")
        if not os.path.exists(path):
            warnings.append(f"rich presence: missing {os.path.relpath(path, ROOT)}")
            continue
        src = open(path, "r", encoding="utf-8").read()
        if f'"Language" "{steam}"' not in src and f'"Language"\t"{steam}"' not in src:
            errors.append(f"rich presence {steam}: Language line must name {steam!r}")
        for tok in RP_TOKENS:
            m = re.search(r'"' + re.escape(tok) + r'"\s+"([^"]*)"', src)
            if not m or not m.group(1).strip():
                errors.append(f"rich presence {steam}: token {tok} missing or empty")
            elif tok == "#Status_Cleaning" and "%clean%" not in m.group(1):
                errors.append(f"rich presence {steam}: {tok} must contain %clean%")


def check_sources(strings, errors, warnings):
    keys = {s["key"] for s in strings}
    used = set()
    for t in uxml_texts():
        used.add(t)
        if t not in keys and not re.fullmatch(r"[A-Z0-9]|Esc|Refluff", t):
            errors.append(f"UXML text is not a key (it would stay English): {t!r}")
    for fn, t in code_literals():
        used.add(t)
        if t not in keys:
            errors.append(f"{fn}: Loc literal is not a key: {t!r}")
    for t in credits_lines():
        used.add(t)
    for t in uxml_texts(named=True):
        used.add(t)
    extra = [s["key"] for s in strings if s["key"] not in used and s["group"] not in ("tools", "hints", "payslip")]
    for k in extra:
        warnings.append(f"key not found in UXML / Loc calls / Credits.txt (stale?): {k!r}")


def cmd_check(codes):
    strings, _ = master()
    en = lang_path("en")
    if not os.path.exists(en) or list(load_json(en).keys()) != [s["key"] for s in strings]:
        print("ERROR en.json is out of date: run `python Tools/loc/loc_tool.py sync`")
        return 1
    errors, warnings = [], []
    langs = [l for l in LANGS if l[0] != "en" and (not codes or l[0] in codes)]
    for code, _, _, _, script in langs:
        check_lang(code, script, strings, errors, warnings)
    if not codes:
        check_sources(strings, errors, warnings)
        check_rich_presence(errors, warnings)
        miss = uncovered("".join(l[2] for l in LANGS if l[4] == "base"), "base")
        if miss:
            errors.append(f"language names without a glyph in Inter: {''.join(sorted(miss))!r}")
    for w in warnings:
        print("warn ", w)
    for e in errors:
        print("ERROR", e)
    print(f"{len(langs)} language(s) checked: {len(errors)} error(s), {len(warnings)} warning(s)")
    return 1 if errors else 0


def cmd_langs():
    for code, name, native, steam, script in LANGS:
        present = os.path.exists(lang_path(code))
        print(f"{code:8} {steam:11} {script:7} {'ok ' if present else '-- '} {name} / {native}")
    return 0


def main(argv):
    try:
        sys.stdout.reconfigure(encoding="utf-8")   # language names and translations on a cp1252 console
    except Exception:
        pass
    if len(argv) < 2 or argv[1] in ("-h", "--help"):
        print(__doc__)
        return 0
    if argv[1] == "sync":
        return cmd_sync()
    if argv[1] == "check":
        return cmd_check(argv[2:])
    if argv[1] == "langs":
        return cmd_langs()
    print("unknown command", argv[1])
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
