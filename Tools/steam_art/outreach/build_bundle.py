"""Prepare local outreach drafts. Does not send messages or upload anything.

After the user supplies keys and uploads PUBLIC_REFLUFF_CREATOR_KIT:
  python Tools/steam_art/outreach/build_bundle.py --media-url HTTPS_URL --keys-file PRIVATE_FILE
Assignments persist by creator id; rerunning never rotates a creator's key.
"""
import argparse
import hashlib
import json
import re
import shutil
import sys
import zipfile
from email.message import EmailMessage
from email.policy import SMTP
from pathlib import Path
from urllib.parse import urlencode, urlparse

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "SteamStore/outreach/2026-10-18"
PUBLIC = OUT / "PUBLIC_REFLUFF_CREATOR_KIT"
PRIVATE = OUT / "PRIVATE_DO_NOT_UPLOAD"
STEAM = "https://store.steampowered.com/app/5304930/"
TOKEN = "[[PUBLIC_MEDIA_URL]]"


def creator(id, name, hello, email, route, contact, hook, wave, note="", language="en"):
    return dict(id=id, name=name, hello=hello, email=email, route=route, contact=contact,
                hook=hook, wave=wave, note=note, language=language)


CREATORS = [
    creator("indiegamejoe", "Indie Game Joe", "Joe", "info@indiegamejoe.com", "email",
            "https://www.reddit.com/r/IndieDev/comments/1umjof3/how_to_pitch_your_game_to_me_indie_game_joe/",
            "A quick indie discovery for you: a filthy rug, a squeegee, and a pattern emerging from the sludge.", 1,
            "Для письма с ключом — email. Предпочитаемый автором Discord show-off-your-game публичный: ключ туда не вставлять."),
    creator("cgdannyb", "CGDannyB", "Danny", "", "private developer form", "https://cgdannyb.com/contact/",
            "Refluff could fit your indie discoveries: it turns carpet-cleaning videos into a playable ASMR sim.", 1,
            "Developer Contact Form. Проверить, что отправка приватная; не использовать публичный комментарий."),
    creator("becsters", "Becsters", "Becsters", "becsterstwitch@outlook.com", "email",
            "https://bsky.app/profile/becsters.bsky.social",
            "I thought Refluff might fit your PowerWash-style satisfying-sounds and ASMR content.", 1),
    creator("steamexplays", "steamexplays", "steamexplays", "steamexplays@gmail.com", "email",
            "https://linktr.ee/steamexplays",
            "A visually unusual Steam indie for your discoveries: cleaning a rug until its hidden pattern returns.", 1),
    creator("cozyteagames", "Cozy Tea Games", "Cozy Tea Games", "CozyTeaGames@gmail.com", "email",
            "https://linktr.ee/cozyteagames",
            "Refluff could fit your bite-sized cozy recommendations: a relaxing cleaning sim with a clear before-and-after payoff.", 1,
            "Конкретный пример TikTok исторический; проверить последние публикации перед отправкой."),
    creator("cozygamingclub", "The Cozy Gaming Club", "Ellie", "cozygamingclub@gmail.com", "email",
            "https://www.cozygamingclub.co.uk/about",
            "For your lesser-known cozy game discoveries: Refluff is about bringing filthy rugs back to life, one satisfying pass at a time.", 1,
            "Конкретный пример TikTok исторический; проверить последние публикации перед отправкой."),
    creator("justkaalin", "JustKaalin", "Kaalin", "justkaalin@gmail.com", "email",
            "https://bsky.app/profile/justkaalin.bsky.social",
            "Refluff might be an unusual addition to your indie-game recommendations: carpet-cleaning ASMR you can play.", 1,
            "Проверить текущий короткий формат; свежий конкретный ролик не установлен."),
    creator("cozyreni", "CozyReni", "Reni and team", "CozyReni@lunyone.de", "email via management",
            "https://creators.lunyone.de/influencers/cozyreni/",
            "I thought Refluff could fit Reni's cozy indie recommendations: a calming carpet-cleaning sim built around tactile before-and-after moments.", 1,
            "Контакт менеджмента. Условия неизвестны; письмо не заказывает платное размещение."),
    creator("jubileewhispers", "JubileeWhispers", "Allison (Jubilee)", "jubileewhispers@gmail.com", "email",
            "https://www.youtube.com/@JubileeWhispers/about",
            "I thought Refluff might suit your gaming ASMR: water, foam, brushes and vacuum sounds, with no music needed.", 2,
            "Email открыт на официальном YouTube 19.10.2026 после CAPTCHA пользователя. Tripo раскрыт в письме из-за AI-политики канала."),
    creator("imjustjarrod", "imjustjarrod", "Jarrod", "", "private media-kit inquiry",
            "https://www.jestr.gg/justjarrod/mediakit",
            "Refluff could make a compact visual indie discovery for your short-form videos: filthy carpet in, fluffy pattern out.", 2,
            "Опубликовано $300 / short на трёх площадках, актуальность цены не установлена. Письмо просит редакционное рассмотрение, ничего не заказывает."),
    creator("cozyk", "Cozy K", "Kennedy and team", "cozyk@hermanaagency.com", "email via management",
            "https://itscozyk.com/contact",
            "A small wind-down game for your cozy audience: put on headphones and restore a dirty carpet with water, foam and a brush.", 2,
            "Тематика шире игр; текущий конкретный игровой short не проверен."),
    creator("paytonscorner", "Payton'sCorner", "Payton", "thepaytonscorner@gmail.com", "email",
            "https://bsky.app/profile/paytonscorner.bsky.social",
            "Refluff could fit your cozy game recommendations: a low-pressure cleaning sim with satisfying textures and real machine recordings.", 2,
            "Наличие игрового TikTok подтверждено; свежий рекомендательный short не проверен."),
    creator("papapupp", "Papa Pupp", "Papa Pupp", "papapuppttv@gmail.com", "email",
            "https://link.me/papapupp",
            "If your audience enjoys satisfying tidy-up games, Refluff offers a carpet-cleaning take on that small-task, visible-payoff feeling.", 2,
            "Смежная аудитория головоломок/cozy, прямое cleaning-соответствие слабее."),
    creator("unauyo", "unauyo / ManeChiku", "まね築様", "manechiku.unauyo@gmail.com", "email",
            "https://manechikuunauyo.wordpress.com/about/", "", 2,
            "Японский. Email опубликован на официальной странице About, проверен 19.10.2026.", "ja"),
    creator("eeowna", "Eeowna", "Eeowna and team", "eeowna@hnmgmt.uk", "email via management",
            "https://hnmgmt.co.uk/",
            "This one is for Windows PC rather than Switch: Refluff is a cozy carpet-cleaning ASMR sim with a satisfying before-and-after loop.", 3,
            "Основной фокус Switch; проверить интерес к PC. Резерв."),
    creator("chromaglitch", "ChromaGlitch", "ChromaGlitch", "chromaglitchgaming@gmail.com", "email",
            "https://chromaglitch.com/contact/",
            "If you're considering cozy PC discoveries, Refluff is a small carpet-cleaning sim focused on satisfying sound and visual progress.", 3,
            "Текущий формат коротких рекомендаций требует проверки. Резерв."),
    creator("eggmilk", "Eggmilk", "Eggmilk", "business.eggmilk@gmail.com", "email",
            "https://eggmilk.carrd.co/#contact",
            "For your cozy and indie gaming side: Refluff is a relaxing carpet-cleaning ASMR sim for Windows PC.", 3,
            "Деловой email опубликован на собственной Carrd-странице; проверен 19.10.2026. Письмо для игрового контента."),
    creator("jestr", "Mahdeen / Jestr", "Mahdeen and Jestr team", "contact@jestr.gg", "email via team",
            "https://www.jestr.gg/",
            "I'm sharing Refluff for consideration, with an upfront note about the AI-content policy you mentioned.", 3,
            "Деловой email на официальном сайте, связанном из u/addit02. Tripo раскрыт; только бесплатное редакционное рассмотрение, без заказа кампании."),
    creator("2bro", "2BRO.", "2BRO. ご担当者様", "", "gated YouTube business email",
            "https://www.youtube.com/user/norunine/about", "", 3,
            "ИСКЛЮЧЁН: описание официального YouTube просит не присылать Steam-ключи по email (19.10.2026).", "ja"),
    creator("jehnotjay", "JehNotJay", "JehNotJay", "jehnotjay@gmail.com", "email",
            "https://www.youtube.com/@JehNotJay",
            "Your invitation for PC simulation and cozy games sounded relevant to Refluff, a carpet-cleaning ASMR sim.", 3,
            "Связка Reddit -> YouTube/Linktree подтверждена; деловой email в описании собственного канала (19.10.2026)."),
]


def write(path, content):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def steam_link(id):
    return STEAM+"?"+urlencode(dict(utm_source=id, utm_medium="creator_outreach", utm_campaign="refluff_prelaunch_nov2026"))


def load_config(a):
    p = PRIVATE / "campaign_config.json"
    cfg = json.loads(p.read_text(encoding="utf-8")) if p.exists() else {
        "sender_name": "Vahov Games", "sender_email": "", "media_url": "",
        "release_window": "November 2026", "keys_in_first_message": True}
    if a.media_url:
        u = urlparse(a.media_url)
        if u.scheme != "https" or not u.netloc or any(ch in a.media_url for ch in "\r\n"):
            raise ValueError("Media URL must be a public HTTPS link.")
        cfg["media_url"] = a.media_url
    if a.sender_email:
        if not re.fullmatch(r"[^\s<>@]+@[^\s<>@]+\.[^\s<>@]+", a.sender_email):
            raise ValueError("Invalid sender email")
        cfg["sender_email"] = a.sender_email
    write(p, json.dumps(cfg, ensure_ascii=False, indent=2)+"\n")
    return cfg


def assign_keys(keys_file, recipients=None):
    p = PRIVATE / "key_assignments.json"
    old = json.loads(p.read_text(encoding="utf-8")) if p.exists() else {}
    if len(set(old.values())) != len(old):
        raise ValueError("Duplicate keys in stored assignments; no drafts changed.")
    if keys_file:
        source = Path(keys_file).resolve()
        if source.is_relative_to(PUBLIC.resolve()):
            raise ValueError("The keys file must stay outside the public media folder.")
        keys = []
        for row in source.read_text(encoding="utf-8-sig").splitlines():
            value = row.strip().upper()
            if not value or value.startswith("#"):
                continue
            if not re.fullmatch(r"[A-Z0-9]{5}(?:-[A-Z0-9]{5}){2,4}", value):
                raise ValueError("Keys file: use one Steam key per line; remove headers and comments.")
            keys.append(value)
        if len(set(keys)) != len(keys):
            raise ValueError("The supplied key list contains duplicates; no drafts changed.")
        unused = [k for k in keys if k not in set(old.values())]
        selected = set(recipients) if recipients is not None else {c["id"] for c in CREATORS}
        if selected - {c["id"] for c in CREATORS}:
            raise ValueError("Unknown creator id in recipient selection")
        pending = [c for c in CREATORS if c["id"] not in old and c["id"] in selected]
        for c, k in zip(pending, unused):
            old[c["id"]] = k
        write(p, json.dumps(old, ensure_ascii=False, indent=2)+"\n")
    return old


def message(c, cfg, keys):
    key = keys.get(c["id"], "[[STEAM_KEY:"+c["id"]+"]]")
    url = steam_link(c["id"])
    media = cfg["media_url"] or TOKEN
    editorial = c["id"] in cfg.get("editorial_only_recipient_ids", [])
    if c["language"] == "ja":
        intro = "お掃除ゲームやASMRの動画に合いそうだと思い、ご連絡しました。"
        body = f"""{c['hello']}

{intro}Vahov Gamesの『Refluff: Carpet Cleaning ASMR』は、汚れたカーペットを水や泡、ブラシ、スクイージーで洗い、模様とふわふわの毛並みを取り戻すPCゲームです。

Windows / Steam向けに2026年11月発売予定で、日本語UIにも対応しています。

ご紹介用のSteamキー: {key}

Steam: {url}
トレーラー・縦型動画・編集用素材（視聴／ダウンロード）: {media}

ゲームの音のみで、音楽やナレーションのない素材も用意しました。ご興味がありましたら、短い動画などでご紹介いただけると幸いです。掲載の義務はありません。
{('今回は有料広告の依頼ではなく、無償でのご紹介をご検討いただければ幸いです。' if editorial else '')}

ありがとうございます。
{cfg['sender_name']}
"""
        subject = "Refluff — カーペット掃除ASMRゲーム / Steamキーのご案内"
    else:
        extra = ""
        if editorial:
            extra += "\nThis is an unpaid editorial submission, not a sponsored-placement request.\n"
        if c["id"] == "jestr":
            extra += ("\nDisclosure: some cleaning-tool head models were generated with Tripo and edited by hand. "
                     "This is also disclosed on the Steam page. Please only consider it if that fits your editorial policy.\n")
        if c["id"] == "jubileewhispers":
            extra += ("\nFor transparency: some cleaning-tool head models were generated with Tripo and edited by hand; "
                      "this is also disclosed on Steam. Please only consider the game if that fits your editorial policy.\n")
        body = f"""Hi {c['hello']},

{c['hook']}

Refluff: Carpet Cleaning ASMR is coming to Windows / Steam in November 2026. Scrape the crust, foam and scrub, push out the muddy water, then dry the pile until it fluffs up. The water and foam are simulated, with real machine recordings for the sound.
{extra}
Your Steam key: {key}

Steam: {url}
Trailer + vertical clips + editing footage (watch/download): {media}

The kit includes a 20-second vertical edit without captions or voice, plus gameplay cutaways. If it fits your channel, I'd love to see your take on it. No obligation to post.

Thanks,
{cfg['sender_name']}
"""
        subject = "Refluff: carpet-cleaning ASMR for your channel — Steam key"
    # One key slot/assignment in each initial letter, even for forms and private DMs.
    assert body.count(key) == 1
    return subject, body


def public_docs():
    write(PUBLIC / "START_HERE.md", f"""# Refluff: Carpet Cleaning ASMR — creator kit

Turn a filthy carpet into a clean, fluffy rug: scrape off crust, soak the pile, foam and scrub, squeegee away the slurry, then dry the fibers. A relaxing single-player cleaning sim with flowing water, foam and recorded machine sounds.

**Steam / wishlist:** {STEAM}

**Developer / publisher:** Vahov Games. **Platform:** Windows PC, mouse and keyboard, DirectX 12. **Planned release:** November 2026 (Steam listing checked 18 October 2026). **Interface:** 31 languages, including English and Japanese. Exact release day and launch price are not announced in this kit. Steam keys are supplied individually in private messages; they are not included in this shared folder.

## Start with these

- [20-second vertical clean edit](01_START_HERE/refluff_clean_edit_20s_vertical.mp4): 1080×1920, 60 fps, no captions, no logo, no voiceover, no music. Good for your own introduction, captions and edit.
- [60-second trailer](01_START_HERE/refluff_trailer_60s_november_2026.mp4): 1920×1080, 60 fps, English captions and November 2026 end card.
- [25.7-second captioned vertical clip](01_START_HERE/refluff_captioned_25p7s_vertical.mp4): 1080×1920, 60 fps, English captions, no voiceover or music.
- [Gameplay cutaways](02_GAMEPLAY_BROLL/): five separate clips, 1920×1080, 60 fps, no added captions, voiceover, logo or music. Dry vacuum, foam, rotary brush, squeegee and wet vacuum. Normal playback speed.
- [Screenshots](03_SCREENSHOTS/) and [transparent logos](04_LOGOS/).

## Capture notes

The vertical close-ups use a cinematic camera, staged lighting and higher carpet shell density. The landscape cutaways come from the fixed game camera with a 16:9 crop. They are recorded gameplay, with HUD hidden for editing, not a continuous full session. All supplied videos retain the game's sound; cutaway audio is reconstructed from the captured simulation's audio-state log. No voiceover or music has been added. "Clean edit" means clean of captions/branding, not silent audio. The trailer also uses cinematic views and editing.

There are ten tools, randomly soiled rugs, earnings and tool upgrades. There are no enemies or a countdown fail state. For the current release information and full requirements, use the Steam page.

## Coverage and credits

These files are supplied for coverage of Refluff. You can cut the supplied game footage, add your commentary/captions and use the logos to identify the game in that coverage. Please link the Steam page and keep the title as **Refluff: Carpet Cleaning ASMR**. Independent opinions are welcome; receiving a key does not require a post or a positive review. For questions or additional material, reply to the message that supplied this kit.

Audio uses credited third-party recordings. Attribution details are in [AUDIO_CREDITS.txt](AUDIO_CREDITS.txt); retain the relevant attribution when reusing that audio. No standalone sound library is included.

AI asset disclosure, also shown on Steam: some cleaning-tool head models were created with an AI 3D model generator and edited by hand. The game does not generate content with AI while playing.
""")
    write(PUBLIC / "FACTS_AND_COPY.txt", f"""REFLUFF: CARPET CLEANING ASMR
Developer / publisher: Vahov Games
Steam: {STEAM}
Platform: Windows PC / Steam; mouse and keyboard; DirectX 12
Planned release: November 2026 (checked 18 October 2026)
Single player; interface in 31 languages

ONE LINE
A carpet-cleaning ASMR sim where water, foam and a set of tools bring filthy rugs back to life.

SHORT DESCRIPTION
Scrape the crust, soak and foam the pile, scrub, push out the muddy water, and dry the fibers until they fluff up. Clean another rug, get paid and upgrade your tools. Best heard with headphones.

OPTIONAL CAPTION
Filthy in. Fluffy out. Refluff: Carpet Cleaning ASMR is coming to Steam in November 2026. Wishlist: {STEAM}

EDITORIAL NOTES
The vertical macro footage uses a cinematic camera and staged lighting; the separate landscape cutaways use the fixed game camera. No exact release day, price, review score, demo availability or embargo is stated in this kit. Keys are sent individually by the developer, never included in this public folder.
""")


def drafts(cfg, keys):
    tracker_path = PRIVATE / "TRACKER.json"
    previous = {r["id"]: r for r in json.loads(tracker_path.read_text(encoding="utf-8"))} if tracker_path.exists() else {}
    rows = []
    overview = ["# Рассылка Refluff — рабочий список", "",
                "Фактическая отправка отмечена SENT и sent_at. Не отправлять повторно. PRIVATE_DO_NOT_UPLOAD содержит персональные ключи.", "",
                "| № | Автор | Волна | Канал | Готовность | Условие перед отправкой |", "|---|---|---|---|---|---|"]
    for i, c in enumerate(CREATORS, 1):
        subject, body = message(c, cfg, keys)
        stem = f"{i:02}_{c['id']}"
        unresolved = re.findall(r"\[\[.*?\]\]", body)
        holds = []
        if c["wave"] == 3:
            holds.append("CHECK_RESERVE")
        if c["id"] == "2bro":
            holds.append("MISSING_CONTACT")
        if c["id"] == "jestr":
            holds.append("AI_POLICY")
        if c["id"] == "jehnotjay":
            holds.append("CHANNEL_UNVERIFIED")
        state = "NEEDS_LINK_OR_KEY" if unresolved else ("HOLD" if holds else "READY_FOR_REVIEW")
        if previous.get(c["id"], {}).get("sent_at"):
            state = "SENT"
            holds = []
        elif c["id"] in cfg.get("excluded_recipient_ids", {}):
            state = "EXCLUDED_" + cfg["excluded_recipient_ids"][c["id"]]
            holds = [cfg["excluded_recipient_ids"][c["id"]]]
        header = f"Автор: {c['name']}\nКанал: {c['route']}\nКонтакт: {c['email'] or c['contact']}\nСтатус: {state}\nПримечание: {c['note'] or 'Перед отправкой открыть последние публикации автора.'}\n\n"
        write(PRIVATE / "drafts" / (stem+".txt"), header+"SUBJECT: "+subject+"\n\n"+body)
        if c["email"]:
            msg = EmailMessage(policy=SMTP)
            msg["To"] = c["email"]
            if cfg["sender_email"]:
                msg["From"] = f"{cfg['sender_name']} <{cfg['sender_email']}>"
            msg["Subject"] = subject
            msg["X-Unsent"] = "1"
            msg.set_content(body)
            (PRIVATE / "drafts" / (stem+".eml")).write_bytes(msg.as_bytes())
        short = body  # Kept complete: the user's key must be in forms/DMs too.
        if not c["email"]:
            write(PRIVATE / "private_forms_and_dm" / (stem+".txt"), header+"SUBJECT: "+subject+"\n\n"+short)
        follow = (f"Hi {c['hello']},\n\nJust following up once on Refluff: Carpet Cleaning ASMR. "
                  f"Here are the gameplay clips and trailer again: {cfg['media_url'] or TOKEN}\n"
                  f"Steam: {steam_link(c['id'])}\n\nIf it isn't a fit, no worries. Thanks for taking a look.\n{cfg['sender_name']}\n")
        if c["language"] == "ja":
            follow = f"{c['hello']}\n\n先日ご案内した『Refluff: Carpet Cleaning ASMR』について、一度だけご連絡いたします。\n動画素材: {cfg['media_url'] or TOKEN}\nSteam: {steam_link(c['id'])}\n\nご興味がありましたら幸いです。どうぞよろしくお願いいたします。\n{cfg['sender_name']}\n"
        write(PRIVATE / "followups_after_7_days" / (stem+".txt"), follow)
        row = dict(previous.get(c["id"], {}))
        row.update({k: v for k, v in c.items() if k != "hook"})
        row.update(number=i, state=state, key_assigned=c["id"] in keys, unresolved=unresolved,
                   holds=holds, steam_url=steam_link(c["id"]),
                   key_fingerprint=(hashlib.sha256(keys[c["id"]].encode()).hexdigest()[:12] if c["id"] in keys else ""))
        for field in ("sent_at", "response", "next_action"):
            row.setdefault(field, "")
        rows.append(row)
        overview.append(f"| {i} | [{c['name']}]({c['contact']}) | {c['wave']} | {c['route']} | {state} | {c['note'] or 'Проверить свежие публикации'} |")
    write(PRIVATE / "TRACKER.json", json.dumps(rows, ensure_ascii=False, indent=2)+"\n")
    write(PRIVATE / "TRACKER.md", "\n".join(overview)+"\n")
    return rows


def manifests_and_archive(keys):
    files = []
    for p in sorted(PUBLIC.rglob("*")):
        if not p.is_file() or p.name == "MANIFEST.json":
            continue
        if p.suffix.lower() in {".txt", ".md", ".json", ".eml", ".html"}:
            text = p.read_text(encoding="utf-8", errors="replace")
            assert "[[STEAM_KEY:" not in text
            assert not any(k in text for k in keys.values()), "Key found in public material"
        assert p.suffix.lower() in {".mp4", ".png", ".jpg", ".md", ".txt", ".json"}
        files.append(dict(file=p.relative_to(PUBLIC).as_posix(), bytes=p.stat().st_size,
                          sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    write(PUBLIC / "MANIFEST.json", json.dumps({"generated": "2026-10-18", "files": files}, indent=2)+"\n")
    archive = OUT / "REFLUFF_PUBLIC_CREATOR_KIT.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_STORED) as z:
        for p in sorted(PUBLIC.rglob("*")):
            if p.is_file():
                z.write(p, p.relative_to(OUT))
    with zipfile.ZipFile(archive) as z:
        assert all(n.startswith("PUBLIC_REFLUFF_CREATOR_KIT/") for n in z.namelist())
        assert z.testzip() is None
    return archive, files


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--media-url")
    ap.add_argument("--keys-file")
    ap.add_argument("--sender-email")
    ap.add_argument("--recipients", help="comma-separated creator ids eligible for new key assignment")
    a = ap.parse_args()
    PRIVATE.mkdir(parents=True, exist_ok=True)
    # Validate key input before changing ready drafts. Values are never printed.
    keys = assign_keys(a.keys_file, None if a.recipients is None else [x.strip() for x in a.recipients.split(",") if x.strip()])
    cfg = load_config(a)
    public_docs()
    rows = drafts(cfg, keys)
    archive, mediafiles = manifests_and_archive(keys)
    shutil.copy2(ROOT / "reports/Авторы коротких видео для Refluff.md", PRIVATE / "RESEARCH_AND_SOURCES.md")
    sent_count = sum(bool(r.get("sent_at")) for r in rows)
    excluded_count = sum(r["state"].startswith("EXCLUDED_") for r in rows)
    write(OUT / "START_HERE_RU.md", f"""# Пакет для рассылки Refluff

Актуальная отправка: **{sent_count} обращений отправлено**, **{excluded_count} исключены**. Подробности — PRIVATE_DO_NOT_UPLOAD/TRACKER.md и SECOND_WAVE_SENT.md. Не отправлять повторно SENT и EXCLUDED. Остальные разделы объясняют подготовку и обновление пакета.

## Что готово

- **PUBLIC_REFLUFF_CREATOR_KIT/** — только публичные материалы. Загрузи эту папку в хранилище с просмотром и скачиванием по ссылке без запроса доступа. Альтернатива — архив **REFLUFF_PUBLIC_CREATOR_KIT.zip**; для просмотра удобнее папка с отдельными MP4.
- **PRIVATE_DO_NOT_UPLOAD/** — 20 персональных писем, {sum(bool(c['email']) for c in CREATORS)} email-копий .eml, тексты для приватных форм/DM, одно напоминание каждому и рабочий список. Здесь находятся персональные ключи. Эту папку и родительскую папку целиком не загружать в общую ссылку.
- Письма используют ноябрь 2026 и индивидуальные Steam UTM-ссылки. Ключ встроен в первое письмо, по одному уникальному ключу на автора. Подпись — Vahov Games; адрес отправителя выбирается в почтовом клиенте или задаётся параметром.

## Как обновить ссылку и ключи

Актуальное наличие ключей и ссылки записано в TRACKER.json и campaign_config.json. Не придумывать ключи и не брать их из посторонних файлов. Для нового ключа — одна строка, без заголовка; хранить за пределами публичной папки. Параметр --recipients ограничивает назначения выбранными id.

Из корня проекта:

```powershell
python Tools/steam_art/outreach/build_bundle.py --media-url "https://ССЫЛКА-НА-ПУБЛИЧНУЮ-ПАПКУ" --keys-file "C:/путь/к/приватному/keys.txt"
```

Можно отдельно передать `--sender-email "your@email.com"`. Если ключей меньше 20, они назначаются по порядку списка (сначала первая восьмёрка), остальные письма остаются неподготовленными. При повторном запуске назначения сохраняются по id автора, перестановка входного списка не меняет ранее назначенный ключ. Дубликаты во входном списке останавливают обработку. Скрипт печатает только количество назначений, сами ключи — никогда.

После выполнения открыть **PRIVATE_DO_NOT_UPLOAD/TRACKER.md**: READY_FOR_REVIEW означает, что ссылка и ключ вставлены; HOLD — что остаётся вопрос по каналу, контакту или AI-политике. Это не автоматическое разрешение на отправку. Фактические адрес, доступность канала и актуальный формат нужно проверить перед письмом.

## Первая волна

1. Indie Game Joe — email с ключом. Его Discord show-off-your-game публичный, туда ключ не публиковать.
2. CGDannyB — приватная Developer Contact Form.
3. Becsters — email.
4. steamexplays — email.
5. Cozy Tea Games — email.
6. The Cozy Gaming Club — email.
7. JustKaalin — email; проверить свежий формат.
8. CozyReni — email менеджмента.

Отправлять индивидуально. Вести sent_at/response/next_action в TRACKER.json либо отметки рядом с TRACKER.md. Для одного автора не дублировать одновременно email и DM. Follow-up — один раз через 7 дней после фактической отправки, если нет ответа; автоматическая рассылка и напоминания не настроены.

## Резерв и доступ

Актуальные ограничения и подтверждённые контакты — в TRACKER.json. ImJustJarrod исключён из бесплатной волны; 2BRO просит не присылать ключи по email. Ключ допустим только в приватном письме, форме или личном сообщении. Для открытого комментария сначала запросить приватный контакт, ключ не вставлять.

Общие материалы не содержат ключей и списка контактов. Перед рассылкой проверь публичную ссылку в окне без авторизации и доступность скачивания. Для дорелизной записи ключ должен давать доступ к сборке до релиза: проверь один выделенный тестовый ключ на отдельном подходящем аккаунте или его тип в Steamworks; не расходуй назначенный автору ключ на тест. Дата эмбарго не согласована и в письмах не выдумана.

## Файлы и проверка

Технический отчёт: PRIVATE_DO_NOT_UPLOAD/QA_REPORT.json. Источники контактов: PRIVATE_DO_NOT_UPLOAD/RESEARCH_AND_SOURCES.md. Карточка фактов и указания к материалам — PUBLIC_REFLUFF_CREATOR_KIT/START_HERE.md. Первая волна не требует готовности запасных контактов.

Скрипт только обновляет локальный пакет; он не отправляет письма и не загружает материалы. Фактическая отправка записывается отдельно в TRACKER.json.
""")
    summary = dict(creators=len(rows), email_drafts=sum(bool(c["email"]) for c in CREATORS),
                   keys_assigned=len(keys), media_url_set=bool(cfg["media_url"]),
                   ready_for_review=sum(r["state"] == "READY_FOR_REVIEW" for r in rows),
                   archive_bytes=archive.stat().st_size, public_files=len(mediafiles)+1,
                   public_contains_contacts_or_keys=False,
                   sent=any(bool(r.get("sent_at")) for r in rows),
                   sent_count=sum(bool(r.get("sent_at")) for r in rows),
                   email_sent_count=sum(bool(r.get("sent_at")) and bool(r["email"]) for r in rows),
                   form_sent_count=sum(bool(r.get("sent_at")) and not r["email"] for r in rows))
    write(PRIVATE / "PREPARATION_STATUS.json", json.dumps(summary, indent=2)+"\n")
    print(json.dumps(summary))


if __name__ == "__main__":
    main()
