"""Builds the bundled UI fonts for the non-Latin languages (Docs/LOCALIZATION.md 4).

  python Tools/loc/build_fonts.py

Sources: Tools/loc/fonts_src/*-VF.ttf, the Google Fonts builds of Noto Sans JP / KR / SC / TC / Thai / Arabic (SIL OFL
1.1; download commands in LOCALIZATION.md 4). Each is instanced to static Regular (400) and Bold (700) and subset to
  ASCII + common punctuation + the script's everyday characters (JIS level 1 kanji, GB2312 level 1, Big5 frequent,
  KS X 1001 Hangul, the Thai / Arabic blocks) + every character its languages' Resources/Loc/<code>.json use,
so a translation edit that stays within everyday characters needs no rebuild; `loc_tool.py check` reports a missing
glyph otherwise. Output: Assets/CarpetCleaning/UI/Fonts/NotoSans<Script>-<Regular|Bold>.ttf. Then run the Unity menu
CarpetCleaning/Localization/Build Font Assets (LocFontBuilder) once if the font assets do not exist yet.

Latin, Greek, Cyrillic and Vietnamese stay on Unity's built-in Inter; nothing is built for them.
Deterministic: same sources + same translations = byte-identical fonts (the head.modified stamp is pinned).
"""
import json
import os
import sys

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SRC = os.path.join(ROOT, "Tools", "loc", "fonts_src")
OUT = os.path.join(ROOT, "Assets", "CarpetCleaning", "UI", "Fonts")
LOC = os.path.join(ROOT, "Assets", "CarpetCleaning", "Localization", "Resources", "Loc")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from loc_tool import LANGS  # noqa: E402

PINNED_TIME = 3913142400   # head.created / head.modified (seconds since 1904): keeps rebuilds byte-identical


def rng(a, b):
    return set(range(a, b + 1))


COMMON = (rng(0x20, 0x7E) | {0xA0, 0xA9, 0xAB, 0xAE, 0xB0, 0xB7, 0xBB, 0xD7, 0xF7}
          | rng(0x2010, 0x2027) | {0x2030, 0x2032, 0x2033, 0x2039, 0x203A, 0x20AC, 0x2116, 0x2122, 0x2212}
          | rng(0x2190, 0x2193) | rng(0x200B, 0x200F) | {0x2060, 0xFEFF})


def decode_rows(codec, leads, trails):
    out = set()
    for lead in leads:
        for trail in trails:
            try:
                s = bytes([lead, trail]).decode(codec)
            except UnicodeDecodeError:
                continue
            if len(s) == 1:
                out.add(ord(s))
    return out


CJK_PUNCT = rng(0x3000, 0x303F) | rng(0xFF01, 0xFF60) | rng(0xFFE0, 0xFFE6) | rng(0x30FB, 0x30FC)
SCRIPT_SETS = {
    # JIS X 0208 level 1 kanji (2,965) + kana
    "ja": lambda: decode_rows("euc_jp", range(0xB0, 0xD0), range(0xA1, 0xFF)) | rng(0x3040, 0x30FF) | rng(0x31F0, 0x31FF) | CJK_PUNCT,
    # KS X 1001 Hangul syllables (2,350) + compatibility jamo
    "ko": lambda: decode_rows("euc_kr", range(0xB0, 0xC9), range(0xA1, 0xFF)) | rng(0x3131, 0x318E) | CJK_PUNCT,
    # GB2312 level 1 (3,755)
    "sc": lambda: decode_rows("gb2312", range(0xB0, 0xD8), range(0xA1, 0xFF)) | CJK_PUNCT,
    # Big5 frequently used characters (5,401)
    "tc": lambda: decode_rows("big5", range(0xA4, 0xC7), list(range(0x40, 0x7F)) + list(range(0xA1, 0xFF))) | CJK_PUNCT,
    "thai": lambda: rng(0x0E00, 0x0E7F),
    "arabic": lambda: rng(0x0600, 0x06FF),
}
FONTS = {"ja": "NotoSansJP", "ko": "NotoSansKR", "sc": "NotoSansSC", "tc": "NotoSansTC", "thai": "NotoSansThai",
         "arabic": "NotoSansArabic"}
WEIGHTS = (("Regular", 400), ("Bold", 700))


def used_chars(script):
    """Every character of the language files written in this script, plus the drop-down's native names."""
    out = set()
    for code, _, native, _, sc in LANGS:
        if sc != script:
            continue
        out |= {ord(c) for c in native}
        path = os.path.join(LOC, code + ".json")
        if os.path.exists(path):
            with open(path, "r", encoding="utf-8") as f:
                for v in json.load(f).values():
                    out |= {ord(c) for c in v}
        else:
            print(f"  note: {code}.json not written yet; only the everyday set goes into {FONTS[script]}")
    return out


def set_names(font, family, style, weight):
    name = font["name"]
    ps = (family + "-" + style).replace(" ", "")
    full = family + " " + style
    for rec in list(name.names):
        if rec.nameID in (1, 2, 3, 4, 6, 16, 17, 21, 22, 25):
            name.removeNames(nameID=rec.nameID)
    for nid, val in ((1, family), (2, style), (3, ps + ";subset;Refluff"), (4, full), (6, ps)):
        name.setName(val, nid, 3, 1, 0x409)
        name.setName(val, nid, 1, 0, 0)
    os2 = font["OS/2"]
    os2.usWeightClass = weight
    os2.fsSelection = (os2.fsSelection & ~0b1100001) | (0b100000 if weight >= 700 else 0b1000000)
    font["head"].macStyle = 1 if weight >= 700 else 0


def build(script):
    base = FONTS[script]
    src = os.path.join(SRC, base + "-VF.ttf")
    if not os.path.exists(src):
        print("ERROR missing", src, "(see LOCALIZATION.md 4 for the download)")
        return False
    uni = COMMON | SCRIPT_SETS[script]() | used_chars(script)
    for style, weight in WEIGHTS:
        vf = TTFont(src)
        axes = {"wght": weight}
        if any(a.axisTag == "wdth" for a in vf["fvar"].axes):
            axes["wdth"] = 100
        font = instancer.instantiateVariableFont(vf, axes)
        family = vf["name"].getDebugName(16) or vf["name"].getDebugName(1)
        family = family.replace(" Thin", "").replace(" Regular", "").strip()
        set_names(font, family, style, weight)
        opts = subset.Options()
        opts.layout_features = ["*"]          # Arabic joining, Thai mark placement, CJK locl / vert: keep every feature
        opts.name_IDs = ["*"]
        opts.name_languages = ["*"]
        opts.notdef_outline = True
        opts.glyph_names = False
        opts.hinting = False                  # the SDF atlas never uses TrueType hinting
        opts.drop_tables += ["DSIG", "STAT"]
        sub = subset.Subsetter(opts)
        sub.populate(unicodes=sorted(uni))
        sub.subset(font)
        font["head"].created = PINNED_TIME
        font["head"].modified = PINNED_TIME
        os.makedirs(OUT, exist_ok=True)
        out = os.path.join(OUT, f"{base}-{style}.ttf")
        font.save(out, reorderTables=True)
        cmap = font.getBestCmap()
        missing = sorted(c for c in used_chars(script) if c not in cmap and c > 0x20)
        print(f"{os.path.relpath(out, ROOT)}: {len(cmap)} chars, {font['maxp'].numGlyphs} glyphs, "
              f"{os.path.getsize(out) // 1024} KB" + (f", MISSING {''.join(chr(c) for c in missing)!r}" if missing else ""))
    return True


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ok = all([build(s) for s in FONTS])
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
