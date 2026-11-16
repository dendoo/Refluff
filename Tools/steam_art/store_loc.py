"""Refluff Steam store page localization: translator briefs, checks and the Steamworks upload files.

Source of truth:
  SteamStore/localization/source_en.json   English fields + context + limits (one entry per translatable piece)
  SteamStore/localization/<code>.json      one translation per language: {"code", "steam", "fields": {id: text}}
  SteamStore/localization/steam_extras.json  description asset (group) name -> Steam's index for its alt text
                                           (the indices of the English export, matched by alt text)
Outputs:
  SteamStore/localization/_brief/glossary_<code>.json   the game's own UI terms in that language (translator input)
  SteamStore/localization/upload/storepage_<steam>.json Steamworks "Upload Localization" files (same keys as its export)

Usage:
  python Tools/steam_art/store_loc.py glossary          write the translator glossaries
  python Tools/steam_art/store_loc.py check [codes]     validate translations (0 errors expected)
  python Tools/steam_art/store_loc.py build [codes]     check, then write the upload files
  python Tools/steam_art/store_loc.py show <code>       print the assembled About This Game BBCode of one language
The header images are drawn by make_headers.py, which reads the hdr_* fields from the same translation files.
Docs: Assets/CarpetCleaning/Docs/STORE_LOCALIZATION.md.
"""
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
LOCDIR = os.path.join(ROOT, "SteamStore", "localization")
GAME_LOC = os.path.join(ROOT, "Assets", "CarpetCleaning", "Localization", "Resources", "Loc")
EXPORT_EN = os.path.join(LOCDIR, "steamworks_export", "storepage_1337056_english.json")
ITEMID = "1337056"                     # Steamworks store item id of app 5304930

# (game Loc code, Steam API language, English name). Steam's store has no Arabic, so the game's 31 languages
# minus Arabic and English = the 29 "store localization" items Steamworks recommends.
LANGS = [
    ("fr", "french", "French"), ("it", "italian", "Italian"), ("de", "german", "German"),
    ("es", "spanish", "Spanish - Spain"), ("es-419", "latam", "Spanish - Latin America"),
    ("pt-BR", "brazilian", "Portuguese - Brazil"), ("pt-PT", "portuguese", "Portuguese - Portugal"),
    ("ro", "romanian", "Romanian"), ("nl", "dutch", "Dutch"), ("da", "danish", "Danish"),
    ("no", "norwegian", "Norwegian"), ("sv", "swedish", "Swedish"), ("fi", "finnish", "Finnish"),
    ("pl", "polish", "Polish"), ("cs", "czech", "Czech"), ("hu", "hungarian", "Hungarian"),
    ("tr", "turkish", "Turkish"), ("ru", "russian", "Russian"), ("uk", "ukrainian", "Ukrainian"),
    ("bg", "bulgarian", "Bulgarian"), ("el", "greek", "Greek"), ("ja", "japanese", "Japanese"),
    ("ko", "koreana", "Korean"), ("zh-Hans", "schinese", "Simplified Chinese"),
    ("zh-Hant", "tchinese", "Traditional Chinese"), ("th", "thai", "Thai"), ("vi", "vietnamese", "Vietnamese"),
    ("id", "indonesian", "Indonesian"), ("ms", "malay", "Malay"),
]
STEAM = {c: s for c, s, _ in LANGS}
STEAM["en"] = "english"
CJK = {"ja", "zh-Hans", "zh-Hant"}      # no spaces between sentences; ko uses spaces
HDR_MAX = {"cjk": 13, "ko": 13, "th": 24, "default": 28}
HEADERS = ["hdr_reveal", "hdr_water", "hdr_rugs", "hdr_paid"]

# The game's UI terms a translator must reuse (keys of Tools/loc/strings.json).
GLOSSARY_KEYS = [
    "Scraper", "Dry vacuum", "Hose", "Foam sprayer", "Brush", "Squeegee", "Wet vacuum", "Dryer", "Detergent", "Mop",
    "Hand over", "Upgrades", "UPGRADES", "Tune your kit", "POWER", "AREA", "POWER works faster",
    "AREA covers more per stroke", "Speed bonus", "Spotless", "Bone dry", "Combo", "COMBO!", "Base pay",
    "CARPET DELIVERED", "INSPECTION", "Moisture highlight", "Wet pile and puddles glow blue", "Dirt highlight",
    "Every trace of soil glows amber", "CARPET", "FLOOR", "CLEAN", "DRY", "Filthy", "Heavy soil",
    "Scrape the dry crust off", "Wet it down", "Brush the mud loose", "Squeegee the dirty water off",
    "Wet-vac the water and foam out", "Dry it", "Sparkling clean!", "Handed over to the client!",
    "Fresh carpet coming in...", "Owned: press {0} to switch it on and off", "the job clock is paused while you shop",
    "Money and upgrades are saved as you play.", "Thank you for playing.",
    "The five rugs are photographs of historic carpets from museum open-access collections (public domain / CC0):",
]
LEGAL_INTRO = "Sound effects from Freesound.org, cut and levelled for this game:"
LEGAL_MORE = ("Additional sounds: Mixkit (mixkit.co) and Freesound users kyles, richwise, RossJuterbock, crcavol, "
              "yehdawgo (CC0).")
# Removed fields (still tolerated in old translation files). sysreq_min_notes was "Requires a 64-bit processor and
# operating system.", which Steam already prints above every requirements block (64-bit ticked): uploaded empty.
DROPPED = {"sysreq_min_notes"}
# Processor lines are product names only: every language gets the English text.
FIXED = {
    "sysreq_min_processor": "Intel Core i5-8400 / AMD Ryzen 5 2600",
    "sysreq_rec_processor": "Intel Core i7-10750H / AMD Ryzen 7 5800X",
}
# Tokens that must survive translation (field -> list of alternatives lists; each inner list = one OR-group).
KEEP = {
    "short_description": [["Refluff"], ["ASMR"]],
    "bullet_tools": [["10"]],
    "bullet_input": [["31"]],
    "bullet_dx12": [["DirectX 12"]],
    "alt_clip5_paid": [["120"]],
    "sysreq_min_osversion": [["Windows 10"], ["1909"], ["64"]],
    "sysreq_min_graphics": [["DirectX 12"], ["GTX 1060"], ["RX 580"], ["4"], ["6"], ["8"], ["VRAM"]],
    "sysreq_min_soundcard": [["DirectX"]],
    "sysreq_rec_osversion": [["Windows 11"], ["64"]],
    "sysreq_rec_graphics": [["RTX 3060"], ["RX 6600 XT"]],
    "sysreq_rec_soundcard": [["DirectX"]],
    "sysreq_rec_notes": [["1440p"], ["60"]],
}
FORBIDDEN = re.compile(r'["<>\[\]{}&\t\r\n]|https?://|  ')


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def source():
    return {f["id"]: f for f in load(os.path.join(LOCDIR, "source_en.json"))["fields"]}


def game_loc(code):
    return load(os.path.join(GAME_LOC, code + ".json"))


def translation(code):
    if code == "en":
        return {"code": "en", "steam": "english", "fields": {k: f["en"] for k, f in source().items()}}
    return load(os.path.join(LOCDIR, code + ".json"))


def hdr_limit(code):
    return HDR_MAX["cjk"] if code in CJK else HDR_MAX.get(code, HDR_MAX["default"])


def header_asset(name, code):
    """File base name of a localized header image. Steamworks groups `<name>_<steam language>` uploads with `<name>`
    into one localized image group: every language's BBCode references `<name>` and Steam serves the viewer's
    language, and the group keeps one alt-text slot per language (Docs/STORE_LOCALIZATION.md 3)."""
    return name if code == "en" else f"{name}_{STEAM[code]}"


# ---- glossary ----------------------------------------------------------------------------------------------------
def cmd_glossary():
    out = os.path.join(LOCDIR, "_brief")
    os.makedirs(out, exist_ok=True)
    notes = {s["key"]: s for s in load(os.path.join(ROOT, "Tools", "loc", "strings.json"))["strings"]}
    for code, steam, name in LANGS:
        g = game_loc(code)
        terms = [{"en": k, "yours": g[k], "where": notes[k]["where"]} for k in GLOSSARY_KEYS if k in g]
        doc = {"language": name, "code": code, "steam": steam,
               "_about": "The game's own UI text in this language (already shipped in the game). Reuse these terms "
                         "whenever the store text names the same thing, so the store page matches what the player "
                         "sees in the game. They also show how the game addresses the player (formal / informal).",
               "terms": terms}
        with open(os.path.join(out, f"glossary_{code}.json"), "w", encoding="utf-8") as f:
            json.dump(doc, f, ensure_ascii=False, indent=1)
    print(f"{len(LANGS)} glossaries -> {os.path.relpath(out, ROOT)}")


# ---- checks -------------------------------------------------------------------------------------------------------
def check(code, strict=True):
    """Returns (errors, warnings) of one translation."""
    src = source()
    err, warn = [], []
    path = os.path.join(LOCDIR, code + ".json")
    if not os.path.exists(path):
        return [f"missing file {os.path.relpath(path, ROOT)}"], []
    try:
        t = translation(code)
    except json.JSONDecodeError as e:
        return [f"bad JSON: {e}"], []
    if t.get("code") != code or t.get("steam") != STEAM[code]:
        err.append(f"header: code/steam must be {code!r}/{STEAM[code]!r}")
    fields = t.get("fields", {})
    for k in src:
        if not str(fields.get(k, "")).strip():
            err.append(f"{k}: missing or empty")
    for k in fields:
        if k not in src and k not in DROPPED:
            err.append(f"{k}: unknown field")
    g = game_loc(code)
    for k, v in fields.items():
        if k not in src or not isinstance(v, str) or not v.strip():
            continue
        f = src[k]
        n = len(v)
        lim = hdr_limit(code) if k in HEADERS else f["max"]
        if k == "short_description":
            lim = 300
        if n > lim:
            (err if k in HEADERS or k == "short_description" else warn).append(f"{k}: {n} chars > {lim}")
        if v != v.strip():
            err.append(f"{k}: leading/trailing whitespace")
        m = FORBIDDEN.search(v)
        if m:
            err.append(f"{k}: forbidden {m.group(0)!r} (no ASCII double quotes, brackets, braces, &, <, >, URLs, "
                       f"tabs, newlines or double spaces)")
        for group in KEEP.get(k, []):
            if not any(tok in v for tok in group):
                err.append(f"{k}: must keep {' / '.join(group)!r}")
        if k in HEADERS and v.rstrip()[-1:] in ".。．":
            err.append(f"{k}: header ends with a full stop")
        if code in CJK and re.search(r"[぀-ヿ一-鿿][,.;:!?]", v):
            warn.append(f"{k}: ASCII punctuation after a CJK character (use full-width)")
        if code == "fr" and re.search(r"\S[:;!?](\s|$)", v) and not re.search(r"https?:", v):
            warn.append(f"{k}: French needs a no-break space (U+00A0 / U+202F) before : ; ! ?")
    pb = fields.get("paid_body", "")
    for key in ("POWER", "AREA"):
        if g[key] not in pb:
            err.append(f"paid_body: must contain the game's {key} term {g[key]!r}")
    return err, warn


def cmd_check(codes):
    total = 0
    for code in codes:
        e, w = check(code)
        total += len(e)
        status = "OK" if not e else f"{len(e)} error(s)"
        print(f"{code:8} {status}" + (f", {len(w)} warning(s)" if w else ""))
        for x in e:
            print(f"   ERROR {x}")
        for x in w:
            print(f"   warn  {x}")
    print(f"{len(codes)} languages, {total} errors")
    return total


# ---- assembly -----------------------------------------------------------------------------------------------------
def img(name):
    return f"[img]{{STEAM_APP_IMAGE}}/extras/{name}[/img]"


def about_bbcode(code):
    f = translation(code)["fields"]
    join = "" if code in CJK else " "
    blocks = [
        img("refluff_clip3_tools") + "\r\n" + f"[b]{f['tools_lead']}[/b]{join}{f['tools_body']}",
        img("hdr_reveal") + "\r\n" + img("refluff_clip1_reveal") + "\r\n" + f["reveal_body"],
        img("hdr_water") + "\r\n" + img("refluff_clip2_water") + "\r\n" + f["water_body"],
        img("hdr_rugs") + "\r\n" + img("refluff_clip4_rugs") + "\r\n" + f["rugs_body"],
        img("hdr_paid") + "\r\n" + img("refluff_clip5_paid") + "\r\n" + f["paid_body"],
        f"[h2]{f['headphones_h2']}[/h2]\r\n{f['headphones_body']}",
        "[list]\r\n" + "\r\n".join("[*]" + f[k] for k in
                                   ("bullet_sim", "bullet_tools", "bullet_nofail", "bullet_input", "bullet_dx12"))
        + "\r\n[/list]",
    ]
    return "\r\n\r\n".join(blocks)


def legal(code):
    en = load(EXPORT_EN)["app[content][legal]"]
    if code == "en":
        return en
    g = game_loc(code)
    f = translation(code)["fields"]
    out = en.replace(LEGAL_INTRO, g[LEGAL_INTRO]).replace(LEGAL_MORE, g[LEGAL_MORE])
    out = out.replace("\r\nLicences: ", "\r\n" + f["legal_licences"] + " ")
    assert out.count("freesound.org/s/") == en.count("freesound.org/s/"), "legal lines lost a credit"
    return out


def upload_doc(code, extras):
    f = dict(translation(code)["fields"])
    f.update(FIXED)
    f.update({k: "" for k in DROPPED})
    doc = {"itemid": ITEMID, "language": STEAM[code], "app[content][legal]": legal(code),
           "app[content][about]": about_bbcode(code), "app[content][short_description]": f["short_description"]}
    for tier in ("min", "rec"):
        for part in ("osversion", "processor", "graphics", "soundcard", "notes"):
            doc[f"app[content][sysreqs][windows][{tier}][{part}]"] = f[f"sysreq_{tier}_{part}"]
    alts = {"refluff_clip3_tools": f["alt_clip3_tools"], "refluff_clip1_reveal": f["alt_clip1_reveal"],
            "refluff_clip2_water": f["alt_clip2_water"], "refluff_clip4_rugs": f["alt_clip4_rugs"],
            "refluff_clip5_paid": f["alt_clip5_paid"]}
    for h in HEADERS:
        alts[h] = f[h]
    missing = [a for a in alts if a not in extras]
    if missing:
        raise SystemExit(f"{code}: steam_extras.json has no index for {missing}")
    for name, text in sorted(alts.items(), key=lambda kv: extras[kv[0]]):
        doc[f"app[assets][extra][{extras[name]}][alt_text]"] = text
    return doc


def cmd_build(codes):
    if cmd_check([c for c in codes if c != "en"]):
        raise SystemExit("fix the errors first")
    extras = load(os.path.join(LOCDIR, "steam_extras.json"))
    out = os.path.join(LOCDIR, "upload")
    os.makedirs(out, exist_ok=True)
    for code in codes:
        doc = upload_doc(code, extras)
        path = os.path.join(out, f"storepage_{ITEMID}_{STEAM[code]}.json")
        with open(path, "w", encoding="utf-8", newline="") as fh:
            json.dump(doc, fh, ensure_ascii=False, separators=(",", ":"))
        print(f"{os.path.relpath(path, ROOT)}  about {len(doc['app[content][about]'])} chars, "
              f"short {len(doc['app[content][short_description]'])}, {sum('alt_text' in k for k in doc)} alt texts")


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    a = sys.argv[1:]
    if not a:
        raise SystemExit(__doc__)
    cmd, rest = a[0], a[1:]
    allc = [c for c, _, _ in LANGS]
    if cmd == "glossary":
        cmd_glossary()
    elif cmd == "check":
        sys.exit(1 if cmd_check(rest or allc) else 0)
    elif cmd == "build":
        cmd_build(rest or ["en"] + allc)
    elif cmd == "show":
        print(about_bbcode(rest[0]).replace("\r\n", "\n"))
        print("\n--- short:", translation(rest[0])["fields"]["short_description"])
    else:
        raise SystemExit(__doc__)


if __name__ == "__main__":
    main()
