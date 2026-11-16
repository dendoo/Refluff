# Достижения и облачные сохранения — что сделать в Steamworks

Всё подготовлено 2026-09-30: код в игре, 20 иконок, тексты на 31 языке, файл локализации для загрузки.
Осталось завести это в Steamworks (партнёрский сайт, агент туда без браузера не ходит) и нажать **Publish**.
Подробности и логика: `Assets/CarpetCleaning/Docs/ACHIEVEMENTS.md` (достижения) и `Docs/STEAM.md` 11 (облако).

Порядок работы: **1 → 2 → 3 → 4 → Publish → новая сборка**. До Publish Steam не знает имён, игра просто копит достижения
в сейве и отдаст их Steam при первом запуске после публикации.

**Состояние на 2026-09-30 (агент через Claude in Chrome, всё перепроверено после перезагрузки страниц):**
- ✅ 1. Статистика: `STAT_CARPETS` (ID 1) и `STAT_TOOLS_USED` (ID 2).
- ✅ 2. Все 10 достижений в нужном порядке, с обеими иконками 256×256 и прогрессом у `ACH_REGULARS` / `ACH_FULL_KIT`.
  Steam выдал им блок статистики **3**, поэтому токены — `NEW_ACHIEVEMENT_3_0` … `3_9` (сверено со скачанным
  английским файлом `steamworks_tokens_english.vdf`); `upload/` пересобран с ними.
- ✅ 4. Steam Cloud: квота, Auto-Cloud-строка, галка «developers only» снята.
- ✅ 3. Локализация (с разрешения пользователя): 31 язык отмечен и сохранён, `upload/achievements_loc_all.vdf` загружен
  («310 achievements processed», 0 без токена); скачанные обратно «All Languages» совпали с файлом (620 строк, SHA-256).
- ⬜ 5. **Publish** (жмёт пользователь), затем галки в Supported Features на странице магазина (см. 5), затем новая сборка.

## 1. Статистика (Stats)

App Admin → **Stats & Achievements** → **Stats** → New Stat. Две штуки:

| API Name | Type | Display Name | Set By | Increment Only | Min | Max | Default | Aggregated |
|---|---|---|---|---|---|---|---|---|
| `STAT_CARPETS` | INT | Carpets handed over | Client | ✔ | 0 | 1000000 | 0 | по желанию |
| `STAT_TOOLS_USED` | INT | Different tools used | Client | ✔ | 0 | 10 | 0 | нет |

Max Change оставить пустым. Window не нужен (это для AVGRATE).

## 2. Достижения (Achievements)

App Admin → **Stats & Achievements** → **Achievements** → New Achievement.
**Создавать строго в этом порядке** (0 → 9): Steam нумерует токены локализации по порядку создания.
Везде: Set By = **Client**, Hidden = **нет**. Иконки лежат в `icons/`: «Achieved» = `<API>.png`, «Unachieved» = `<API>_locked.png` (256×256).

| # | API Name | Display Name | Description | Progress Stat |
|---|---|---|---|---|
| 0 | `ACH_FIRST_CARPET` | First Fluff | Hand over your first carpet. | — |
| 1 | `ACH_SPOTLESS` | Spotless | Hand over a dirty carpet 100% clean. | — |
| 2 | `ACH_BONE_DRY` | Bone Dry | Soak a carpet, then hand it over 100% dry. | — |
| 3 | `ACH_COMBO` | Showroom Finish | Hand over a dirty carpet 100% clean and 100% dry. | — |
| 4 | `ACH_FILTHY` | Filthy to Fluffy | Hand over a Filthy carpet 100% clean. | — |
| 5 | `ACH_EXPRESS` | Express Service | Earn the full speed bonus on a heavily soiled carpet. | — |
| 6 | `ACH_REGULARS` | Regular Customers | Hand over 25 carpets. | `STAT_CARPETS`, min 0, max 25 |
| 7 | `ACH_FULL_KIT` | Every Tool in the Van | Use all 10 tools. | `STAT_TOOLS_USED`, min 0, max 10 |
| 8 | `ACH_MAXED_OUT` | Maxed Out | Upgrade one tool's POWER and AREA to MAX. | — |
| 9 | `ACH_INSPECTOR` | Inspector | Buy the Moisture highlight and the Dirt highlight. | — |

Тексты для копирования: `python Tools/Steam/achievements.py show` (английский) или `show ru` (русский).
Как они выглядят по-русски: «Первый пушистик», «Ни пятнышка», «Ни капли», «Хоть на витрину», «Из грязи в пух»,
«Экспресс-чистка», «Постоянные клиенты», «Весь инструмент в деле», «Прокачано до упора», «Инспектор».

Картинка всех иконок сразу (256 / 64 / 32 px на фоне Steam): `SteamStore/_preview/achievements_sheet.png`.

## 3. Локализация (31 язык одним файлом)

App Admin → **Stats & Achievements** → **Achievement Localization**:

1. Отметить языки игры (все 31, как в Supported Languages) и сохранить.
2. Скачать данные локализации (Download Localization Data, язык English или All languages) — это файл `.vdf` с токенами,
   которые Steam выдал нашим 10 достижениям.
3. Выполнить `python Tools/Steam/achievements.py build --tokens <скачанный файл>` — скрипт сопоставит токены с API-именами
   по английскому названию (так тексты не перепутаются, даже если порядок создания где-то съехал) и перепишет файлы.
   Сделано 2026-09-30: токены `NEW_ACHIEVEMENT_3_0` … `3_9` (блок 3, потому что статистика заняла ID 1 и 2), они же теперь
   по умолчанию в скрипте; скачанный файл лежит в `steamworks_tokens_english.vdf`. Шаги 2–3 повторять не надо.
4. Загрузить `upload/achievements_loc_all.vdf` (все языки сразу). Если Steam захочет по одному языку — файлы
   `upload/single/achievements_loc_<язык>.vdf`.

## 4. Облачные сохранения (Steam Cloud)

App Admin → Application → **Steam Cloud** (<https://partner.steamgames.com/apps/cloud/5304930>):

1. Byte quota per user: `1048576`; Number of files allowed per user: `10`. Save.
2. Появится блок **Auto-Cloud** → Root Paths, одна строка:

   | Root | Subdirectory | Pattern | OS | Recursive |
   |---|---|---|---|---|
   | `WinAppDataLocalLow` | `VahovGames/Refluff` | `carpet_save.json` | [All OSes] | нет |

   Шаблон — именно имя файла, не `*`: в той же папке лежат логи Unity, сейв редактора и временные `.tmp`/`.bak`.
3. Root overrides не нужны (игра только под Windows; Steam Deck/Proton пишет в тот же путь внутри префикса).
4. После первого сохранения квоты Steam **сам ставит** галку Beta Testing → «Enable cloud support for developers only».
   С ней облако скрыто от покупателей и Auto-Cloud не синхронизирует. Галка должна быть снята (снята 2026-09-30).

## 5. Publish и сборка

1. App Admin → **Publish** → Prepare for Publishing → Publish to Steam. Публикация касается только настроек приложения
   (достижения, облако); кнопки ревью магазина и сборки она не трогает.
2. Edit Store Page → Basic Info → **Supported Features**: отметить «Steam Achievements», «Steam Cloud» и «Stats»
   (Steam пишет об этом на страницах достижений и облака; до Publish галки помечены «Must first implement…»), сохранить.
3. Нужна новая сборка с кодом достижений (живая v0.9.1 его не содержит): Unity → CarpetCleaning → Steam →
   Build Windows (release, for upload), затем загрузка как обычно (`Docs/STEAM.md` 7).
4. Проверка на своей копии из Steam: сдать ковёр — должен выскочить тост «First Fluff» (если его ещё нет);
   в свойствах игры в клиенте Steam появится облако и «Последняя синхронизация».
   Сбросить свои тестовые достижения: `steam://open/console` → `reset_all_stats 5304930`.

## Файлы

| Путь | Что |
|---|---|
| `source_en.json` | английский источник: порядок, API-имена, статы, заметки переводчикам |
| `loc/<код>.json` | переводы (30 языков + английский из источника) |
| `_review/` | заметки редакторов и аудита смысла |
| `icons/` | 20 иконок 256×256 (`<API>.png` + `<API>_locked.png`) |
| `upload/` | то, что грузится в Steamworks (собирает `Tools/Steam/achievements.py build`) |
| `steamworks_tokens_english.vdf` | английские токены, скачанные из Steamworks 2026-09-30 (`build --tokens` сверяет по ним) |

Пересобрать: иконки `python -B Tools/steam_art/make_achievement_icons.py`, тексты `python Tools/Steam/achievements.py check`
и `build`, проверка всех картинок магазина `python Tools/steam_art/verify_assets.py` (ALL OK).
