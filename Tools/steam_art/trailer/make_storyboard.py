"""Trailer storyboard (Docs/TRAILER.md): real in-game frames, framed the way the trailer will frame them, with the
planned on-screen text drawn over them.

    python Tools/steam_art/trailer/make_storyboard.py

Reads only existing captures under SteamStore/ (store screenshots, _work/shots scene layers, _work/clips takes, the logo)
and writes:
  SteamStore/trailer/storyboard/NN_slug.jpg   one 1280x720 frame per shot (shot number + timecode badge top left)
  SteamStore/trailer/storyboard_sheet.jpg     every shot with its timecode, picture, sound and source status
Deterministic. When a shot gets its real trailer take, point its `src` at a frame of that take and re-run.
The shot list here mirrors the table in Docs/TRAILER.md: change both together.
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[3]
STORE = ROOT / "SteamStore"
WORK = STORE / "_work"
SCR = STORE / "screenshots"
OUT = STORE / "trailer"
FONTS = ROOT / "Tools" / "steam_art" / "fonts"

FW, FH = 1280, 720                 # storyboard frame (2/3 of the 1920x1080 master: sizes below scale by 1.5)
BAR = 4 * 60.0 / 90.0              # edit grid: 90 BPM, 4/4, one bar = 2.667 s
FEST = (12.0, 35.0)                # the part of the trailer a Steam festival hub plays, muted (Docs/TRAILER.md 1)

INK = (26, 33, 43)
TEAL = (20, 174, 160)
WHITE = (255, 255, 255)
STATUS = {                         # tag colour, label
    "ok": ((23, 165, 122), "дубль есть"),
    "restage": ((236, 160, 20), "переснять"),
    "new": ((214, 96, 58), "снять"),
    "edit": ((62, 123, 232), "монтаж"),
}


def clip(take, n):
    return WORK / "clips" / take / ("f%04d.png" % n)


def shot_layer(name):
    return WORK / "shots" / (name + "_scene.png")


C1, C3, C4A, C4B, C5 = "c1_reveal_r3", "c3_tools_r1", "c4_rugs_r3a", "c4_rugs_r3b", "c5_paid_r1_cam"

# bars = (first bar, length in bars) on the 90 BPM grid. cap = on-screen text as it will read in the trailer.
SHOTS = [
    dict(n=1, slug="squeegee_pour", bars=(0, 1), title="Вал на плитку", status="restage",
         src=shot_layer("18_squeegee_floor"), crop=(1280, 380, 2560, 1440),
         what="Сквиджи гонит вал бурой воды по «звезде» и сливает его с края на белую плитку, за лезвием открывается узор. "
              "Крупно, медленный наезд.",
         sound="Хлюпанье резины, вал плещется о кафель. Без текста и музыки.",
         source="скриншот 10 (уже на «звезде»), снять проход как дубль"),
    dict(n=2, slug="pink_swirl", bars=(1, 1), title="Розовый вихрь", status="restage",
         src=clip(C3, 100), crop=(359, 18, 996, 560),
         what="Роторная щётка крутится в розовой пене, пена закручивается и сереет. Крупно.",
         sound="Гул роторной машины, мокрый скраб.",
         source="clip3_tools, но на ковре-герое"),
    dict(n=3, slug="title", bars=(2, 1), title="Название", status="edit",
         src=clip(C4B, 1), crop=(853, 400, 2133, 1200), darken=0.30, blur=2.0, overlay="title",
         what="Логотип Refluff поверх чистого ворса (фон притемнён), под ним плашка «Headphones on».",
         sound="Мягкий «пух» сушилки и искорка, вступает музыка.",
         source="титр в монтаже, фон = дубль #20"),
    dict(n=4, slug="arrival", bars=(3, 1), title="Прилёт", status="ok",
         src=clip(C4A, 45), crop=None, cap="Filthy rugs come in.",
         what="Грязный ковёр прилетает сверху и ложится на плитку. Полный кадр, в финале с HUD (инструменты, CLEAN 0 %, $).",
         sound="Свист полёта, мягкий шлепок ткани о плитку.",
         source="clip4 дубль A + слой HUD"),
    dict(n=5, slug="the_dirt", bars=(4, 1), title="Грязь", status="ok",
         src=clip(C4A, 82), crop=(740, 860, 1920, 1080), cap="Ten tools. One right order.",
         what="Медленная панорама крупно по грязи: корка засохшей грязи, серая пыль, пятна.",
         sound="Тишина комнаты, лёгкий хруст.",
         source="clip4 дубль A после посадки"),
    dict(n=6, slug="scrape", bars=(5, 1), title="1 SCRAPE", status="restage",
         src=clip(C3, 30), crop=(436, 0, 1244, 700), cap="SCRAPE", chip="1",
         what="Скребок ломает корку, пыль облачками, куски скачут по плитке.",
         sound="Скрежет, хруст корки, стук кусков по кафелю.",
         source="clip3 / скриншот 02"),
    dict(n=7, slug="vacuum", bars=(6, 1), title="2 VACUUM", status="new",
         src=clip(C3, 55), crop=(900, 100, 780, 439), cap="VACUUM", chip="2",
         what="Сухой пылесос чистит полосу: крошки исчезают в сопле, пыльная плёнка уходит. (Заглушка: полоса до пылесоса.)",
         sound="Мотор и дребезг крошек в трубе, точно по кадру. Только после замены звука на Dyson.",
         source="дубля нет; у пылесоса только кольцо"),
    dict(n=8, slug="soak", bars=(7, 1), title="3 SOAK", status="restage",
         src=shot_layer("03_hose_spill"), crop=(720, 430, 2560, 1440), cap="SOAK", chip="3",
         what="Шланг: мокрые пятна расплываются, пыль на глазах становится грязью, летят капли-бусины.",
         sound="Шипение струи, плеск.",
         source="clip2 / скриншот 05"),
    dict(n=9, slug="foam", bars=(8, 1), title="4 FOAM", status="restage",
         src=clip(C3, 75), crop=(134, 0, 1244, 700), cap="FOAM", chip="4",
         what="Пенный распылитель насыпает розовый холм пены на полосу.",
         sound="Шипение баллона, шорох пузырьков.",
         source="clip3 / clip2"),
    dict(n=10, slug="scrub", bars=(9, 1), title="5 SCRUB", status="restage",
         src=clip(C3, 130), crop=(418, 0, 1244, 700), cap="SCRUB", chip="5",
         what="Роторная щётка (350 об/мин) взбивает пену с грязью в серую кашу.",
         sound="Гул полотёра, мокрая щетина.",
         source="clip3"),
    dict(n=11, slug="squeegee", bars=(10, 1), title="6 SQUEEGEE", status="restage",
         src=clip(C3, 170), crop=(436, 0, 1244, 700), cap="SQUEEGEE", chip="6",
         what="Сквиджи проходит полосу цепочки: вал грязи катится перед лезвием и переваливает через край, за лезвием "
              "открывается узор. Ракурс и полоса другие, чем в #01.",
         sound="Резиновое хлюпанье, слив на кафель. Музыка выпадает: только звук.",
         source="clip3 (заглушка: вал розовый)"),
    dict(n=12, slug="extract", bars=(11, 1), title="7 EXTRACT", status="ok",
         src=clip(C1, 180), crop=(0, 0, 1600, 900), cap="EXTRACT", chip="7",
         what="Моющий пылесос забирает остатки: узор яркий, ворс тёмный от влаги и в полосах.",
         sound="Всасывание, бульканье в баке.",
         source="clip1, второй проход"),
    dict(n=13, slug="refluff", bars=(12, 1), title="8 REFLUFF", status="new",
         src=shot_layer("07_dryer_finish"), crop=(1000, 690, 1920, 1080), cap="REFLUFF", chip="8", accent=True,
         what="Сушилка: тёмные примятые влажные полосы светлеют, ворс встаёт. Титр крупнее остальных. (Заглушка: сушилка на другом ковре.)",
         sound="Разгон вентилятора, поток воздуха, шорох ворса. Музыка возвращается.",
         source="главный риск: читается ли подъём ворса"),
    dict(n=14, slug="before_after", bars=(13, 1), title="До / после", status="ok",
         src=[clip(C4A, 82), clip(C4B, 1)], crop=[(175, 199, 3490, 1963), (175, 199, 3490, 1963)], split=0.52,
         what="Весь ковёр: шторка из грязного состояния в чистое. Камера одна, оба состояния настоящие. Без текста.",
         sound="Звенящий блеск, подъём музыки.",
         source="clip4: последний кадр A + первый кадр B"),
    dict(n=15, slug="hand_over", bars=(14, 1), title="Сдача", status="ok",
         src=SCR / "07_handover_flight.jpg", crop=None, cap="Hand it over.",
         what="Чистый ковёр в золотых искрах взлетает и уходит к клиенту. HUD в кадре.",
         sound="Искристый звон, свист.",
         source="clip4 дубль B / скриншот 07"),
    dict(n=16, slug="pay_slip", bars=(15, 1), title="Расчёт", status="ok",
         src=clip(C5, 110), crop=None, cap="Get paid.", cap_pos="br",
         what="Расчётный лист печатается построчно: база, скорость, Spotless, Bone dry, штамп COMBO, $120, монеты в кошелёк.",
         sound="Тики строк, удар штампа, звон монет.",
         source="clip5 + виртуальная камера"),
    dict(n=17, slug="upgrade", bars=(16, 1), title="Магазин", status="ok",
         src=clip(C5, 215), crop=None, cap="Upgrade your kit.",
         what="«Tune your kit»: покупка POWER для щётки, анимация делений, кошелёк скручивается.",
         sound="Клик, звук покупки.",
         source="clip5"),
    dict(n=18, slug="lamp", bars=(17, 1), title="Лампа", status="restage",
         src=shot_layer("09_lamp_dirt"), crop=(384, 286, 3072, 1728), cap="Find every last spot.",
         what="X: ковёр гаснет, остатки грязи светятся янтарём; затем Z: влажные места светятся синим.",
         sound="Щелчок лампы, низкий гул.",
         source="скриншоты 06 / 09, нужен живой дубль"),
    dict(n=19, slug="five_rugs", bars=(18, 2), title="Пять ковров", status="restage",
         src=[SCR / "04_rya_half_clean.jpg", shot_layer("03_hose_spill"), shot_layer("07_dryer_finish"),
              shot_layer("13_floor_mop")],
         crop=[None, (443, 350, 2954, 1662), (320, 180, 3200, 1800), (0, 228, 2743, 1543)],
         grid=True, cap="Five real museum rugs.",
         what="4 склейки по 1,33 с на сильную долю: риа в пене, туркменский под шлангом, европейский под сушилкой, "
              "швабра на плитке. Второй титр: «A new mess every job.»",
         sound="Каждая склейка вступает своим инструментом.",
         source="скриншоты 04 / 05, кадры 07 / 13"),
    dict(n=20, slug="breath", bars=(20, 1), title="Выдох", status="ok",
         src=clip(C4B, 1), crop=(960, 760, 1920, 1080), cap="Filthy in. Fluffy out.",
         what="Тихий крупный план чистого сухого ворса ковра-героя, медленный наезд. Машины стихли.",
         sound="Затухание сушилки, один мягкий шорох.",
         source="clip4 дубль B в покое + виртуальная камера"),
    dict(n=21, slug="end_card", bars=(21, 2.25), title="Финал", status="edit",
         src=clip(C4B, 1), crop=(384, 316, 3072, 1728), darken=0.45, blur=5.0, overlay="end",
         what="Логотип поверх медленно плывущего чистого ковра, «Wishlist now on Steam», «October 2026», мелко Vahov Games.",
         sound="Музыка разрешается, последние 0,5 с тишина.",
         source="монтаж; дата отдельным слоем"),
]


# ------------------------------------------------------------------------------------------------ fonts
_fonts = {}


def font(name, size, style):
    key = (name, size, style)
    if key not in _fonts:
        f = ImageFont.truetype(str(FONTS / name), size)
        f.set_variation_by_name(style)
        _fonts[key] = f
    return _fonts[key]


def fredoka(size, style="SemiBold"):          # Latin only: the trailer's own text
    return font("Fredoka[wdth,wght].ttf", size, style)


def rubik(size, style="Regular"):             # Cyrillic: the storyboard's notes
    return font("Rubik[wght].ttf", size, style)


# ------------------------------------------------------------------------------------------------ frames
def load(path, crop):
    im = Image.open(path).convert("RGB")
    if crop:
        x, y, w, h = crop
        im = im.crop((x, y, x + w, y + h))
    return fit(im, 16, 9)


def fit(im, aw, ah):
    """Centre-crop to aw:ah."""
    w, h = im.size
    if abs(w * ah - h * aw) <= max(aw, ah):
        return im
    if w * ah > h * aw:
        nw = round(h * aw / ah)
        x0 = (w - nw) // 2
        return im.crop((x0, 0, x0 + nw, h))
    nh = round(w * ah / aw)
    y0 = (h - nh) // 2
    return im.crop((0, y0, w, y0 + nh))


def sized(path, crop, w, h):
    return load(path, crop).resize((w, h), Image.Resampling.LANCZOS)


def background(s):
    if s.get("split"):
        a = sized(s["src"][0], s["crop"][0], FW, FH)
        b = sized(s["src"][1], s["crop"][1], FW, FH)
        x = round(FW * s["split"])
        feather = 14
        mask = Image.new("L", (FW, FH), 0)
        md = ImageDraw.Draw(mask)
        md.rectangle((x, 0, FW, FH), fill=255)
        mask = mask.filter(ImageFilter.GaussianBlur(feather / 2))
        img = Image.composite(b, a, mask)
        d = ImageDraw.Draw(img, "RGBA")
        d.rectangle((x - 1, 0, x + 1, FH), fill=(255, 255, 255, 170))
        return img
    if s.get("grid"):
        gap = 6
        cw, ch = (FW - gap) // 2, (FH - gap) // 2
        img = Image.new("RGB", (FW, FH), (24, 26, 30))
        for i, (p, c) in enumerate(zip(s["src"], s["crop"])):
            img.paste(sized(p, c, cw, ch), ((i % 2) * (cw + gap), (i // 2) * (ch + gap)))
        return img
    img = sized(s["src"], s["crop"], FW, FH)
    if s.get("blur"):
        img = img.filter(ImageFilter.GaussianBlur(s["blur"]))
    if s.get("darken"):
        img = Image.blend(img, Image.new("RGB", img.size, (8, 10, 14)), s["darken"])
    return img


def shadowed(img, box, radius, fill, blur=10, offset=4, alpha=70):
    """Rounded box with a soft drop shadow, drawn onto img (RGB) in place."""
    x0, y0, x1, y1 = box
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle((x0, y0 + offset, x1, y1 + offset), radius, fill=(0, 0, 0, alpha))
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    img.paste(sh, (0, 0), sh)
    top = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(top).rounded_rectangle(box, radius, fill=fill)
    img.paste(top, (0, 0), top)


def text_w(f, t):
    b = f.getbbox(t)
    return b[2] - b[0]


def pill(img, text, chip=None, accent=False, pos="bc", y_bottom=FH - 44):
    """The trailer caption: a HUD-style white pill (teal when accent), optional teal number chip; pos bc / bl / br
    (bottom centre, or a bottom corner when the centre holds UI, like the pay slip)."""
    f = fredoka(52 if accent else 40)
    h = round(f.size * 1.55)
    pad = round(h * 0.42)
    cs = h - 16                                     # chip size
    tw = text_w(f, text)
    w = pad + (cs + 14 if chip else 0) + tw + pad
    x0 = {"bl": 44, "br": FW - 44 - w}.get(pos, (FW - w) // 2)
    y0 = y_bottom - h
    fill = TEAL + (245,) if accent else (255, 255, 255, 238)
    shadowed(img, (x0, y0, x0 + w, y0 + h), h // 2, fill)
    d = ImageDraw.Draw(img)
    x = x0 + pad
    if chip:
        cy = y0 + 8
        d.rounded_rectangle((x - 6, cy, x - 6 + cs, cy + cs), 12, fill=WHITE if accent else TEAL)
        cf = fredoka(round(cs * 0.62), "Bold")
        d.text((x - 6 + cs / 2, cy + cs / 2), chip, font=cf, fill=TEAL if accent else WHITE, anchor="mm")
        x += cs + 14 - 6
    d.text((x, y0 + h / 2), text, font=f, fill=WHITE if accent else INK, anchor="lm")


def headphones(d, cx, cy, s, col):
    """A plain headphones glyph (band arc + two cups), s = height in px."""
    r = s * 0.46
    d.arc((cx - r, cy - s * 0.52, cx + r, cy - s * 0.52 + 2 * r), 180, 360, fill=col, width=max(2, round(s * 0.11)))
    cw, ch = s * 0.24, s * 0.42
    for sx in (-1, 1):
        x = cx + sx * (r - cw * 0.35)
        d.rounded_rectangle((x - cw / 2, cy - ch * 0.15, x + cw / 2, cy + ch * 0.85), round(cw * 0.35), fill=col)


def logo(img, width_frac, cy_frac):
    lg = Image.open(STORE / "logo" / "refluff_logo.png").convert("RGBA")
    w = round(FW * width_frac)
    lg = lg.resize((w, round(lg.height * w / lg.width)), Image.Resampling.LANCZOS)
    img.paste(lg, ((FW - w) // 2, round(FH * cy_frac - lg.height / 2)), lg)
    return lg.height


def title_card(img):
    lh = logo(img, 0.60, 0.43)
    f = fredoka(30, "Medium")
    label = "Headphones on"
    h = 56
    icon = 34
    w = 24 + icon + 14 + text_w(f, label) + 26
    x0 = (FW - w) // 2
    y0 = round(FH * 0.43 + lh / 2 + 34)
    shadowed(img, (x0, y0, x0 + w, y0 + h), h // 2, (255, 255, 255, 236))
    d = ImageDraw.Draw(img)
    headphones(d, x0 + 24 + icon / 2, y0 + h / 2, icon, INK)
    d.text((x0 + 24 + icon + 14, y0 + h / 2), label, font=f, fill=INK, anchor="lm")


def end_card(img):
    logo(img, 0.56, 0.36)
    d = ImageDraw.Draw(img)
    f = fredoka(40)
    label = "Wishlist now on Steam"
    h = 70
    w = text_w(f, label) + 72
    x0 = (FW - w) // 2
    y0 = round(FH * 0.63)
    shadowed(img, (x0, y0, x0 + w, y0 + h), h // 2, TEAL + (255,), alpha=90)
    d.text((FW / 2, y0 + h / 2), label, font=f, fill=WHITE, anchor="mm")
    d.text((FW / 2, y0 + h + 44), "October 2026", font=fredoka(34, "Medium"), fill=WHITE, anchor="mm")
    d.text((FW / 2, FH - 34), "Vahov Games", font=fredoka(20, "Regular"), fill=(235, 235, 235), anchor="mm")


def tc(t):
    m, s = divmod(t, 60.0)
    return "%d:%04.1f" % (m, s)


def times(s):
    b0, bl = s["bars"]
    return b0 * BAR, (b0 + bl) * BAR


def in_fest(s):
    t0, t1 = times(s)
    return t0 < FEST[1] and t1 > FEST[0]


def badge(img, s):
    t0, t1 = times(s)
    d = ImageDraw.Draw(img, "RGBA")
    label = "%02d   %s-%s" % (s["n"], tc(t0), tc(t1))
    f = rubik(18, "Medium")
    w = text_w(f, label) + 22
    d.rounded_rectangle((14, 14, 14 + w, 44), 9, fill=(10, 14, 20, 150))
    d.text((25, 29), label, font=f, fill=(240, 240, 240), anchor="lm")


def frame(s):
    img = background(s)
    if s.get("overlay") == "title":
        title_card(img)
    elif s.get("overlay") == "end":
        end_card(img)
    if s.get("cap"):
        pill(img, s["cap"], s.get("chip"), s.get("accent", False), s.get("cap_pos", "bc"))
    badge(img, s)
    return img


# ------------------------------------------------------------------------------------------------ sheet
def wrap(text, f, width):
    lines, cur = [], ""
    for word in text.split():
        t = word if not cur else cur + " " + word
        if text_w(f, t) <= width:
            cur = t
        else:
            lines.append(cur)
            cur = word
    if cur:
        lines.append(cur)
    return lines


def sheet(frames):
    cols, pw, gap = 3, 752, 18
    ph = round(pw * 9 / 16)
    th = 176
    head = 150
    rows = (len(SHOTS) + cols - 1) // cols
    W = cols * pw + (cols + 1) * gap
    H = head + rows * (ph + th + gap) + gap
    img = Image.new("RGB", (W, H), (22, 24, 29))
    d = ImageDraw.Draw(img)
    total = max(times(s)[1] for s in SHOTS)
    d.text((gap, 26), "Refluff: Carpet Cleaning ASMR — трейлер %s, раскадровка v1 (2026-09-26)" % tc(total),
           font=rubik(34, "SemiBold"), fill=(240, 240, 240))
    d.text((gap, 76), "Сетка монтажа 90 BPM: такт = 2,67 с. Кадры настоящие (дубли клипов, скриншоты); у «переснять» и «снять» "
                      "это заглушка до нового дубля. Бирюзовая рамка — окно 0:12–0:35, которое фестиваль Steam крутит без звука.",
           font=rubik(19), fill=(190, 196, 205))
    x = gap
    for key in ("ok", "restage", "new", "edit"):
        col, lab = STATUS[key]
        f = rubik(18, "Medium")
        w = text_w(f, lab) + 24
        d.rounded_rectangle((x, 112, x + w, 138), 8, fill=col)
        d.text((x + 12, 125), lab, font=f, fill=WHITE, anchor="lm")
        x += w + 12
    for i, (s, fr) in enumerate(zip(SHOTS, frames)):
        cx = gap + (i % cols) * (pw + gap)
        cy = head + (i // cols) * (ph + th + gap)
        if in_fest(s):
            d.rounded_rectangle((cx - 5, cy - 5, cx + pw + 4, cy + ph + th + 2), 10, outline=TEAL, width=4)
        img.paste(fr.resize((pw, ph), Image.Resampling.LANCZOS), (cx, cy))
        t0, t1 = times(s)
        y = cy + ph + 10
        hf = rubik(22, "SemiBold")
        d.text((cx, y), "%02d  %s–%s  %s" % (s["n"], tc(t0), tc(t1), s["title"]), font=hf, fill=(245, 245, 245))
        col, lab = STATUS[s["status"]]
        f = rubik(16, "Medium")
        w = text_w(f, lab) + 20
        d.rounded_rectangle((cx + pw - w, y + 2, cx + pw, y + 26), 7, fill=col)
        d.text((cx + pw - w + 10, y + 14), lab, font=f, fill=WHITE, anchor="lm")
        y += 34
        bf = rubik(17)
        for line in wrap(s["what"], bf, pw)[:3]:
            d.text((cx, y), line, font=bf, fill=(214, 218, 224))
            y += 22
        for line in wrap("Звук: " + s["sound"], bf, pw)[:2]:
            d.text((cx, y), line, font=bf, fill=(160, 205, 198))
            y += 22
        d.text((cx, y + 2), "Источник: " + s["source"], font=rubik(15), fill=(140, 146, 156))
    return img


def main():
    (OUT / "storyboard").mkdir(parents=True, exist_ok=True)
    keep = {"%02d_%s.jpg" % (s["n"], s["slug"]) for s in SHOTS}
    for old in (OUT / "storyboard").glob("*.jpg"):     # a renamed or dropped shot leaves no stale frame behind
        if old.name not in keep:
            old.unlink()
    frames = []
    for s in SHOTS:
        fr = frame(s)
        fr.save(OUT / "storyboard" / ("%02d_%s.jpg" % (s["n"], s["slug"])), quality=90, subsampling=0)
        frames.append(fr)
    sh = sheet(frames)
    sh.save(OUT / "storyboard_sheet.jpg", quality=88, subsampling=0)
    total = max(times(s)[1] for s in SHOTS)
    print("%d shots, %s, sheet %dx%d -> %s" % (len(SHOTS), tc(total), sh.width, sh.height, OUT))


if __name__ == "__main__":
    main()
