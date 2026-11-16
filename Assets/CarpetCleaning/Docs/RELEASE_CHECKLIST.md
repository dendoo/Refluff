# Чеклист релиза в Steam — Refluff: Carpet Cleaning ASMR

App ID **5304930**, депот **5304931**, партнёр **Vahov Games** (partner id 424848), пакет **1833640**.
Снято с Steamworks App Admin **2026-09-25**. Источник правды — сама страница:
<https://partner.steamgames.com/apps/landing/5304930>. Этот файл — рабочая копия, чтобы отмечать прогресс;
после каждой загрузки в Steamworks сверяйтесь с живой страницей.

Смежные документы: `STEAM.md` (сборка и заливка билда), `STORE_ART.md` + `SteamStore/README.md` (что куда грузить),
`AUDIO_CREDITS.md` (кредиты CC BY для страницы магазина и титров).

## 0. Общий статус на 2026-09-27 (оба чеклиста закрыты)

| Поле | Значение |
|---|---|
| App Release State | Unavailable |
| Specified date | **Oct 30, 2026, 10:00 GMT+3** |
| Customers see | **October 2026** |
| Пакет 1833640 | Hidden, цена **$3.99** (решение пользователя 2026-09-27), региональные цены — рекомендованные Steam, действующие |
| **Store Presence checklist** | **COMPLETE** («Mark as ready for review» НЕ нажата — это делает пользователь) |
| **Game Build checklist** | **COMPLETE**, 13 из 13 (2026-09-27) |
| Билд на default | **BuildID 25558788** = v0.9.0, депот 5304931, манифест 7836903329287943867, 573 МБ на диске / 165.6 МБ загрузка |
| Launch discount | None |

Дальше только кнопки пользователя: **сначала** «Mark as ready for review» у Store Presence (2–3 дня), **после этого** Steam
открывает «Submit build for review» (3–5 рабочих дней). Пока страница не отправлена на ревью, кнопки ревью билда нет.

## 1. Store Presence — ЗАКРЫТО 2026-09-25

Редактируется в Store Admin: <https://partner.steamgames.com/admin/game/editbyappid/5304930>

### Store

- [x] **Basic Info** — App Type Game, имя, платформа Windows, языки, жанры, теги, контроллер, Legal Lines, support.
- [x] **Descriptions** — Short Description (260 симв.) + About This Game (BBCode, 7 блоков).
      Визуальный редактор снят, BBCode вставлен как есть.
      Кредиты CC BY лежат в **Legal Lines**, не в описании: Steam запрещает внешние ссылки в About This Game.
      **2026-09-26** About This Game заменён: 5 MP4-клипов + 4 заголовка-картинки (1845 симв., 9 тегов `[img]`, см. 2.1).
- [x] **Content Survey** — <https://partner.steamgames.com/contentdescriptors/editsurvey/5304930/829417>.
      Все категории взрослого контента пустые. Рейтинги: Brazil «L», Germany «All Ages», опубликованы.
      Декларация generative AI = **Yes** (головы инструментов из Tripo), AI во время игры = No,
      внешние AI-сервисы = No. Текст декларации видно под About This Game на странице магазина.
- [x] **Planned Release Date** — Oct 30, 2026 (пятница; ранее 12 октября Steam не даёт), публично «October 2026».
- [x] **System Requirements** — были ПУСТЫЕ, хотя чеклист горел зелёным (Steam засчитывает выбор платформы).
      Заполнены min (Win 10 64-bit 1909+, i5-8400 / Ryzen 5 2600, 8 ГБ, DX12 GPU 4 ГБ VRAM — GTX 1060 6 ГБ /
      RX 580 8 ГБ, DX 12, 2 ГБ, звук любой) и rec (Win 11, i7-10750H / Ryzen 7 5800X, 16 ГБ, RTX 3060 /
      RX 6600 XT, 2 ГБ, 1440p 60+ fps). **Минималка расчётная, не протестированная** — мерили только на
      RTX 3070 Ti laptop. Размер билда `Builds/Steam/content/windows` = 549 МБ, поэтому 2 ГБ с запасом.
- [x] **Controller Support Description** — мастер пройден, ответ «Mouse and keyboard only».
      Итог: Status **No Controller Support**.
- [x] **5 Or More Screenshots** — 9 штук из `SteamStore/screenshots/`.
      **2026-09-26 заменены** пересъёмкой под ACES. Новый порядок по гайду: 1 — эффектный, 2 — геймплей, 3 — прогресс,
      4 — эффектный, дальше чередуются, один ковёр два раза подряд не идёт. Слоты: 01 hero reveal, 02 скребок по корке,
      03 расчётный лист, 04 рия наполовину чистая, 05 шланг, 06 лампа влажности, 07 полёт при сдаче, 08 магазин апгрейдов,
      09 сдача чистого ковра. Все 9 отмечены «suitable for all ages», у всех написан alt text (Edit Accessibility Text).
      9 старых (Neutral) удалены. Порядок сверен по SHA1: `ss_<hash>` у Steam = SHA1 загруженного файла.
      Старый набор лежит в `SteamStore/_work/v2_neutral/`.
- [x] **Capsule Images** — из `SteamStore/store/`. Ещё с Neutral-плашек, под ACES не перерендерены (2.1, «Открыто»).
- [x] **Library Assets** — из `SteamStore/library/`. Тоже ещё с Neutral-плашек.
- [x] **Support Info** — email `denisvaho@gmail.com`. Сайта и телефона нет.
- [x] **Developer and Publisher Names** — оба **Vahov Games** (через пробел, по решению пользователя).
      В Unity company name остаётся `VahovGames` без пробела: от него зависит путь сейва
      `LocalLow/VahovGames/Refluff`, менять его нельзя — сейвы игроков переедут.
- [x] **Store Page Descriptive Tags** — жанры Casual (primary) + Indie + Simulation; 16 тегов
      опубликованы через Tag Wizard в порядке приоритета (см. 1.1).

### 1.1 Теги, по весу

`Cleaning, Relaxing, Cozy, Simulation, Job Simulator, Hobby Sim, Singleplayer, Casual, Atmospheric,
Realistic, Physics, Colorful, Family Friendly, Top-Down, 3D, Procedural Generation`

- Порядок не случайный: по гайду <https://steam-izdat.github.io/04-marketing/store-page/> первые 3–5 тегов
  несут наибольший вес в алгоритме, а generic-тег «Indie» в теги брать не надо (в *Genres* он остался).
- Тега **ASMR** в таксономии Steam нет — проверено поиском в мастере. «Satisfying» тоже нет.
- После публикации Steam показал соседей: Rug Cleaning Simulator, Cozy Cleaner, Cozy Organizer,
  Chopping Trees, Black Friday Janitor — значит теги попали в нужную нишу.

### Community

- [x] **App Icon** — `SteamStore/icons/app_icon_184x184.jpg`, залит 2026-09-25.
- [x] **Shortcut Icon** — `SteamStore/icons/refluff.ico`, залит 2026-09-25.

> Обе иконки грузятся на <https://partner.steamgames.com/apps/clientimages/5304930>
> (App Admin → Installation → Client Images). Галочку «Convert shortcut icon to app icon at upload time»
> мы снимали: app icon загружен отдельным готовым JPG 184×184, а не автогенерацией из .ico.
> После загрузки страница пишет «This app has unpublished changes» — иконки появятся в клиенте Steam
> только после **Publish** на вкладке Publish. На чеклист это не влияет, галочки зелёные сразу.

### Recommended (не блокирует релиз)

- [ ] Cloud Saves — <https://partner.steamgames.com/apps/cloud/5304930>.
      Сейв лежит в `LocalLow/VahovGames/Refluff`, так что настраивается дёшево. 2026-09-30: значения готовы
      (квота 1 МБ / 10 файлов, Auto-Cloud `WinAppDataLocalLow` + `VahovGames/Refluff` + `carpet_save.json`) —
      `SteamStore/achievements/README.md` 4, `STEAM.md` 11. Осталось ввести и нажать Publish.
- [ ] Steam Achievements — <https://partner.steamgames.com/apps/achievements/5304930>.
      2026-09-30: в игре 10 достижений + 2 статы, иконки и тексты на 31 языке готовы (`ACHIEVEMENTS.md`);
      в Steamworks ещё не заведены — пошагово `SteamStore/achievements/README.md`, потом Publish и новая сборка.
- [ ] Accessibility features — галочки в `tab_basic#accessibility`.

## 2. Game Build — обязательно, чтобы игра запускалась у покупателей

### Store

- [x] **Platform Support Matches** — после первого Publish 2026-09-27 пункт покраснел: в Steamworks (Application → General)
      не был отмечен ни один OS, зелёным он был, пока конфиг не публиковался. Отмечено Windows + 64-bit only
      (`oslist windows`, `osarch 64`), опубликовано — снова зелёный.
- [x] **Pricing For At Least One Package** — **$3.99** (2026-09-27). Уровень «$3.99» из Price Tiers, остальные 40 валют —
      Multi-variable conversion Steam (£3.59, 4,15 €, CHF 3.59, 200 руб., CDN$ 4.59, A$ 5.75, ¥530, CIS/SASIA/LATAM/MENA $2.69 …).
      Отправку «Save pending prices» с галкой «publish automatically after review» нажимал пользователь: авто-классификатор
      прав агента заблокировал это как production deploy. На `packagelanding/1833640` цены уже «Current».
- [x] **Published Pricing For At Least One Package** — зелёная.
- [x] **Trailer Uploaded** — 2026-09-27, `tab_movies`: «Refluff - Gameplay Trailer», единственный (значит, первый),
      категория Gameplay, Visible on Store, Show before screenshots, suitable for all ages, English, своя обложка
      `refluff_trailer_cover.png` (путь миниатюры на CDN = SHA1 обложки `cfdbc78d…`). Файл в браузере совпал с мастером
      по SHA-256 (`9d86349f…`), сервер перекодировал его за несколько минут (`gettrailerstate` → `published`). Как грузили — `TRAILER.md` 13.
- [x] **App Configuration** — настроено.

### Depots

- [x] **At Least One Depot Configured** — депот 5304931.
- [x] **At Least One Build Configured** — 2026-09-27: v0.9.0 залит через **Web Uploads** (ZIP 176.6 МБ, без steamcmd и пароля),
      BuildID 25558788 поставлен live на `default` (игра не выпущена: скачать могут только владельцы dev comp).
      Порядок и ловушки — `STEAM.md` 7.1.
- [x] **Launch Options Defined** — `Refluff.exe`.
- [x] **Install Directory Set**.
- [x] **Depot Languages Configured**.
- [ ] **Языки (2026-09-27)**: игра переведена на 31 язык (`LOCALIZATION.md`). Нужно: (1) опубликовать галки Interface
      для всех 31 языка (Supported Languages → Publish); (2) залить 29 новых `Tools/Steam/rich_presence_*.vdf`
      (Community → Rich Presence) и Publish; (3) собрать и залить новый билд — в v0.9.0 (BuildID 25558788) переводов нет.
      2026-09-27 вечер: **v0.9.1 собран** (31 язык, проверен ja / ar / de), ZIP `Builds/Steam/upload/Refluff_0.9.1.zip`
      (174.5 МБ) **ждёт загрузки пользователем** — агенту загрузку заблокировал классификатор auto-режима (`SESSION_LOG.md`).
- [x] **Store And Devcomp Packages Match**.
- [x] **All Depots Attached**.

## 2.1 Клипы в описание — ЗАКРЫТО 2026-09-26

Гайд <https://steam-izdat.github.io/04-marketing/store-page/>: первая анимация обязана быть видна **выше сгиба
«Read More»** — 90–95 % посетителей его не раскрывают; на блок фичи — заголовок + анимация + 1–2 строки.

**Почему MP4, а не GIF.** С 2025 года Valve принимает в описании MP4/WEBM: исходник до 100 МБ, анимация короче 12 с.
Steam всё перекодирует сам и на живой странице отдаёт WebM VP9 (`<video autoplay muted loop playsinline>` с постером
AVIF), картинки — AVIF. Колонка описания 780 px, рекомендуемая ширина файла 1170–1560 px. Поэтому «GIF-ки» сделаны
MP4-клипами; GIF остался только запасным вариантом (`make_clips.py --gif`).
Старое правило «один файл ≤ 5 МБ, всего ≤ 15 МБ» лимитом Valve больше не является. Мы держим его как свою цель
по весу страницы: `verify_assets.py` проверяет < 11.5 с, ширину ≥ 780, отсутствие звука, ≤ 5 МБ на файл и ≤ 15 МБ всего.

- [x] **5 клипов** (таблица ниже, в порядке на странице). «Смена инструментов» стоит первой — по просьбе пользователя.
      H.264, без звука, 1560 px в ширину. Каждый прошёл 1–3 круга арт-директора.
- [x] **4 заголовка-картинки** вместо текстовых (совет гайда): `Tools/steam_art/make_headers.py` →
      `SteamStore/description/hdr_*.png`, 1560×120 RGBA — кремовые «ворсистые» буквы Fredoka, бирюзовое строчное
      подчёркивание, розовые пузырьки (лучшее из трёх направлений). `hdr_reveal` «Watch the pattern come back»,
      `hdr_water` «Real water, real mud», `hdr_rugs` «A new rug every job», `hdr_paid` «Get paid, upgrade your kit».
      У первого клипа заголовка нет: `hdr_tools` убран, когда клип «смена инструментов» встал наверх.
- [x] **Вес.** Всё медиа описания вместе с заголовками — 14.66 МБ (цель 15 МБ), `verify_assets` ALL OK.
- [x] **Alt text** у всех 9 ассетов описания: у клипов — одно предложение о том, что в кадре, у заголовков — их текст.
- [x] **Сгиб проверен** на живом превью (store beta mode): блок `#aboutThisGame` до «Read More» — 850 px; клип 1
      на 68–393 px, `hdr_reveal` на 462, клип 2 на 528–853 px — оба клипа выше сгиба. Видео стартуют сами,
      без звука, по кругу.
- [x] **BBCode** — `SteamStore/description/about_this_game.bbcode` (1845 симв., 9 тегов `[img]`). Прежний текст —
      `about_before_2026-09-26.bbcode` (2147 симв., 7 текстовых блоков). Ссылка на ассет в BBCode:
      `{STEAM_APP_IMAGE}/extras/<имя файла без расширения>`. Макет страницы — `SteamStore/_preview/description_mock.png`.

| # на странице | Файл | Что в кадре | Длит. | Размер | Кадр |
|---|---|---|---|---|---|
| 1 | `refluff_clip3_tools.mp4` | один дубль на одном пятне засохшей корки на трансильванском ковре: скребок ломает корку (куски летят на плитку) → пена → роторная щётка → сквиджи сталкивает стену пены за край | 8.76 с | 3.47 МБ | 1560×650 |
| 2 | `refluff_clip1_reveal.mp4` | моющий пылесос вытягивает жижу из выскобленной полосы на анатолийском ковре, два прохода; чистая полоса держится 2.1 с | 9.96 с | 3.46 МБ | 1560×650 |
| 3 | `refluff_clip2_water.mp4` | шланг: три всплеска по сухой пыльной рие → грязное пятно расползается → розовая пена поднимается и оседает | 7.20 с | 2.04 МБ | 1560×650 |
| 4 | `refluff_clip4_rugs.mp4` | чистый трансильванский ковёр с блёстками улетает, прилетает грязный анатолийский; два заказа подряд, жёсткие склейки | 6.56 с | 2.87 МБ | 1560×878 |
| 5 | `refluff_clip5_paid.mp4` | настоящая сдача: расчётный лист печатается построчно (COMBO, $120, монеты), магазин, две покупки | 9.68 с | 2.24 МБ | 1560×878 |

Как снято: реальные кадры `EditorApplication.Step` через `Editor/StoreGifApi.cs`, инструменты ведёт
`ToolController.ScriptedPointer`; кадры → `Tools/steam_art/make_clips.py`. Рецепты — `Tools/steam_art/clips/recipes/<клип>/`,
подробности — `STORE_CLIPS.md`.

**Открыто по странице:**

- [ ] Капсулы, library и key art ещё с Neutral-плашек — под ACES не перерендерены.
- [ ] 10-й скриншот (сквиджи сталкивает жижу с ковра на плитку) снят в 4 круга и **не залит**: под ACES мокрый ворс
      почти чёрный, полосы оставляют края плёнки, вода на полу матовая и читается как тень. Файл —
      `SteamStore/_work/screens/not_uploaded/10_squeegee_floor_round4.jpg`, рецепт —
      `Tools/steam_art/screens/recipes/reshoot_aces_0926/squeegee/`.
- [x] Локализация страницы — 2.2 (2026-09-28).
- [x] Трейлер — загружен 2026-09-27 (2).
- [x] Своп CC BY-NC звука сухого пылесоса — 4 (Dyson, 2026-09-26).
- [x] Баги игры, найденные при съёмке: угол ковра при сдаче уходит под плитку — исправлено (`CarpetHandover.CornerLift`,
      `HANDOVER.md` 10); вода на полу без блеска — исправлено (`RoomReflection.hlsl`, `FLOOR_SYSTEM.md` 5.1), 2026-09-26.

## 2.2 Локализация страницы — ЗАКРЫТО 2026-09-28

Steamworks (App Admin → RECOMMENDED ITEMS) рекомендовал «<язык> store localization» для 29 языков: 31 язык игры минус
английский и арабский (арабского в магазине Steam нет). Сделаны все 29, подробности — `STORE_LOCALIZATION.md`.

- [x] Short Description, About This Game, alt-тексты 9 ассетов, системные требования (мин. и рек.), юридический блок
      (переведены только вводные строки кредитов, сами кредиты CC BY дословно) — на 29 языках, сохранено в Steamworks.
- [x] 4 заголовка-картинки описания на каждом языке (116 PNG, группы локализованных изображений Steam: описание на любом
      языке ссылается на `hdr_reveal` и т. п., Steam сам подставляет картинку языка посетителя).
- [x] Качество: переводчик → независимый редактор → аудит смысла по всем языкам (правки с причинами в
      `SteamStore/localization/_review/<код>.md`); русский вычитан построчно. Носители языков тексты не читали.
- [x] Английский: «English interface» → «interface in 31 languages».
- [x] Supported Languages: было 67 языков с галкой Interface (36 лишних, албанский … зулу), стало ровно 31 (с согласия
      владельца). Страница магазина показывает 31.
- [x] Дубль «Requires a 64-bit processor and operating system.» в «Дополнительно» минималки убран (Steam печатает это сам).
- [ ] Не локализованы: текст раскрытия ИИ (Content Survey), капсулы и скриншоты (английский заголовок и HUD), трейлер.

## 3. Процедура и сроки после того, как чеклисты закрыты

1. Отправить **страницу магазина на ревью** — 2–3 рабочих дня на проход.
2. Залить играбельный билд и отправить **билд на ревью** — 3–5 рабочих дней.
   Билд должен содержать все фичи, описанные на странице; баги допустимы, билд можно обновлять
   во время и после ревью.
3. Страница должна провисеть как **«Coming Soon» минимум 2 недели**.
4. Должно пройти **минимум 21 день с момента оплаты app credit**.
5. Опционально: launch discount (Valve советует 10–15 %; в сезонной распродаже нельзя участвовать
   в течение 30 дней после запуска).

Самая ранняя возможная дата релиза = дата публикации страницы + 2 недели, при условии что оба ревью пройдены.

## 4. Блокеры со стороны проекта (вне Steamworks)

- [x] **Звук сухого пылесоса под CC BY-NC.** С 2026-09-26 играет Dyson (InspectorJ, Freesound 430632, CC BY 4.0);
      Samsung (CC BY-NC) остался на диске только как эталон и не должен попадать в сборку. `ImportShippedRecordings`
      обновлён, `SteamBuild` предупредит, если NC-кредит вернётся в банк. `AUDIO.md` 11.4, `AUDIO_CREDITS.md`.
- [x] **Кредиты CC BY на странице магазина** — лежат в Legal Lines (2026-09-25). В блоке уже стоит строка
      InspectorJ (Dyson): после свопа (2026-09-26) она верна.
- [x] **Кредиты CC BY в титрах игры** — 2026-09-27: меню паузы (Esc / кнопка Menu) → Credits, текст `UI/Credits.txt`;
      он же лежит рядом с exe как `CREDITS.txt`, лицензии библиотек — `THIRD_PARTY_NOTICES.txt` (`PAUSE_MENU.md`).
- [ ] **Пересобрать аудио из оригиналов Freesound.** Сейчас в репозитории лежат превью-версии;
      `Tools/audio_recordings.py fetch|build` воспроизводит сборку бит в бит, но перед релизом
      исходники должны быть полноразмерными.
- [x] **Лицензия Tripo на модели инструментов.** 2026-09-27 пользователь подтвердил: платный тариф / API с кредитами —
      коммерческое использование разрешено (на бесплатном Basic модели публичные под CC BY 4.0, и по ToS права на результат
      остаются у Tripo — так что это было важно). В титрах строка «generated with Tripo AI».
- [x] **Трейлер** — снят и смонтирован 2026-09-27 (`TRAILER.md` 12): 1:00, 21 план на 20 дублях 60 fps, звук — фоли
      игры, два раунда ревью. Осталось загрузить (2).

## 5. История

- **2026-09-25** — снят первый срез чеклистов. Залиты App Icon и Shortcut Icon (секция Community закрыта,
  2 из 2 зелёные).
- **2026-09-25, позже** — закрыт весь раздел Store Presence: Steam показывает **CHECKLIST COMPLETE**
  и предлагает «Mark as ready for review». Кнопка **не нажата** — сначала GIF-ки (2.1), трейлер и замена
  CC BY-NC звука. Осталось только 2 (Game Build) и 4 (блокеры проекта).
- **2026-09-26** — тонмаппинг сцены переключён на **ACES** (решение пользователя; тёмные красители и мокрый ворс
  стали почти чёрными — оставлено как есть). 9 скриншотов пересняты под ACES и залиты в новом порядке, у всех
  «suitable for all ages» и alt text, 9 старых удалены (1). Закрыт 2.1: в About This Game 5 MP4-клипов
  + 4 заголовка-картинки, 14.66 МБ, первые два клипа выше «Read More». **Publish** и **«Mark as ready for review»**
  по-прежнему **не нажаты**. Открыто: капсулы под ACES, 10-й скриншот (сквиджи) не залит, локализация (2.2),
  трейлер, своп CC BY-NC звука, цена, билд.
- **2026-09-27, позже** — закрыт весь Game Build: трейлер загружен (своя обложка, Gameplay, перед скриншотами), цена
  $3.99 (сохранил пользователь), в игре меню паузы с титрами CC BY и выходом, билд v0.9.0 собран, прогнан 45 с на этой
  машине (Steam ready, без исключений), залит через Web Uploads, BuildID 25558788 live на default, Rich Presence
  (en, ru) загружен, два Publish (иконки + RP; затем oslist windows / 64-bit). Оба чеклиста **COMPLETE**.
  «Mark as ready for review» и отправка билда на ревью — за пользователем.
- **2026-09-27** — трейлер снят и смонтирован (`TRAILER.md` 12): мастер `SteamStore/trailer/refluff_trailer_main.mp4`,
  1:00, 1080p60, −14.1 LUFS / −2.5 dBTP, `check_trailer` 18/18 PASS, своя обложка. До съёмки: блеск воды на плитке,
  Dyson вместо Samsung, угол ковра при сдаче, цены в магазине, чипы напольных инструментов, пласты корки. Не загружен:
  «Trailer Uploaded» остаётся открытым до решения пользователя.
