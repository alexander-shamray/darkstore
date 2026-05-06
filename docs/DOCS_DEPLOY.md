# 📤 Как обновить сайт документации

> **Репозиторий:** [https://github.com/alexander-shamray/darkstore](https://github.com/alexander-shamray/darkstore)
> **Активный хостинг:** GitHub Pages → [https://alexander-shamray.github.io/darkstore/](https://alexander-shamray.github.io/darkstore/)
> **Альтернатива:** Azure Static Web Apps (см. **Часть B**)

Документ описывает **два варианта** хостинга статического сайта документации:

- **Часть A — GitHub Pages** (текущий рабочий вариант, бесплатно для публичных репозиториев)
- **Часть B — Azure Static Web Apps** (альтернатива на Azure: интегрируется с остальным стеком, поддерживает приватный доступ через Entra ID на Standard-плане)

Для повседневной работы используй **Часть A**. Часть B нужна, если репозиторий снова станет приватным или потребуется аутентификация.

Общие шаги (редактирование, локальная проверка, добавление новых страниц, типичные проблемы) — в конце документа.

---

## 🅰️ Часть A — GitHub Pages (активный)

> Подходит для **публичного** репозитория. Бесплатно. Деплой ~30–60 секунд.

### A.1. Один раз — включить GitHub Pages

```powershell
gh api -X POST repos/alexander-shamray/darkstore/pages `
  -f "source[branch]=gh-pages" `
  -f "source[path]=/"
```

Или через UI: GitHub → Settings → Pages → Source: **Deploy from a branch** → Branch: `gh-pages` / `(root)`.

> Если включить через API/UI не удаётся («Your current plan does not support GitHub Pages») — репозиторий **приватный**, переключи на Public или используй **Часть B**.

### A.2. Быстро — одна команда (рутинный деплой)

```powershell
cd C:\dev\darkstore
git add docs/
git commit -m "docs: описание что изменил"
python -m mkdocs gh-deploy --force
```

Что делает `mkdocs gh-deploy --force`:
1. Собирает статический сайт в папку `site\`
2. Пушит его в ветку `gh-pages` на GitHub
3. GitHub Pages публикует автоматически

Ожидаемый вывод:

```
INFO  - Documentation built in 1.4 seconds
INFO  - Copying 'site' to 'gh-pages' branch and pushing to GitHub.
INFO  - Your documentation should shortly be available at:
        https://alexander-shamray.github.io/darkstore/
```

### A.3. Опционально — запушить исходники в `main`

Чтобы сохранить исходные `.md`-файлы и историю в репозитории:

```powershell
git push origin main
```

### A.4. Структура веток

> Ветка `main` → исходники (`docs\`, `mkdocs.yml`)
> Ветка `gh-pages` → собранный сайт (обновляется командой `mkdocs gh-deploy`)

---

## 🅱️ Часть B — Azure Static Web Apps (альтернатива)

> Подходит для **приватного** репозитория или когда нужна аутентификация (Entra ID, GitHub login). Free-план — публичный сайт; Standard (~$9/мес) — частный с auth.

### B.1. Один раз — настройка Azure SWA

1. **Создай ресурс** в Azure Portal → Static Web Apps → Create
   - Plan: **Free** (публичный сайт) или **Standard** (приватный, аутентификация Entra ID)
   - Region: ближайший к Sweden Central или West Europe
   - Deployment source: **Other** (workflow создаём вручную, без авто-привязки к репо)
2. **Скопируй deployment token:** ресурс → Overview → "Manage deployment token"
3. **Добавь секрет в GitHub:** Settings → Secrets and variables → Actions → New repository secret
   - Name: `AZURE_STATIC_WEB_APPS_API_TOKEN`
   - Value: токен из шага 2
4. **Создай workflow** `.github/workflows/deploy-docs-swa.yml` (см. раздел B.3 ниже)
5. **Триггер первого деплоя:** GitHub → Actions → "Deploy Docs — Azure Static Web Apps" → Run workflow → main

### B.2. Рутинный деплой через SWA

```powershell
cd C:\dev\darkstore
git add docs/
git commit -m "docs: описание что изменил"
git push origin main
```

GitHub Actions автоматически:
1. Чекаут репозитория
2. Установка Python 3.12 + mkdocs-material
3. Сборка сайта в `site/` (`mkdocs build --strict --clean`)
4. Деплой в Azure SWA через `Azure/static-web-apps-deploy@v1`

Прогресс — на вкладке **Actions**. Деплой занимает ~2–3 минуты.

### B.3. Workflow для SWA (`.github/workflows/deploy-docs-swa.yml`)

```yaml
name: Deploy Docs — Azure Static Web Apps

on:
  push:
    branches: [ main ]
    paths:
      - 'docs/**'
      - 'mkdocs.yml'
      - '.github/workflows/deploy-docs-swa.yml'
  pull_request:
    types: [ opened, synchronize, reopened, closed ]
    branches: [ main ]
    paths:
      - 'docs/**'
      - 'mkdocs.yml'
  workflow_dispatch:

permissions:
  contents: read
  pull-requests: write

jobs:
  build_and_deploy:
    if: github.event_name != 'pull_request' || github.event.action != 'closed'
    runs-on: ubuntu-latest
    timeout-minutes: 10
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with:
          python-version: '3.12'
          cache: 'pip'
      - run: pip install mkdocs-material
      - run: mkdocs build --strict --clean
      - uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: site
          skip_app_build: true
          skip_api_build: true

  close_pull_request:
    if: github.event_name == 'pull_request' && github.event.action == 'closed'
    runs-on: ubuntu-latest
    steps:
      - uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          action: close
```

### B.4. Структура (для SWA)

> Ветка `main` → исходники + workflow `.github/workflows/deploy-docs-swa.yml`
> Azure SWA → собранный сайт (обновляется автоматически через GitHub Actions при push в `main`)

---

## 🆚 Сравнение вариантов

| Параметр | A — GitHub Pages | B — Azure Static Web Apps |
|---|---|---|
| Стоимость (публичный сайт) | Бесплатно | Бесплатно (Free-план) |
| Работает с приватным репо | Только на платном плане GitHub | Да (Free-план) |
| Аутентификация / приватный сайт | Нет | Да (Standard-план, ~$9/мес — Entra ID, GitHub) |
| Команда деплоя | `mkdocs gh-deploy --force` | `git push` (CI/CD) |
| Время деплоя | 30–60 секунд | 2–3 минуты |
| PR-превью | Нет | Да (автоматически) |
| Custom domain | Да | Да |
| Интеграция с Azure стеком проекта | Нет | Да |
| Нужен GitHub Actions secret | Нет | Да (`AZURE_STATIC_WEB_APPS_API_TOKEN`) |

**Текущий выбор:** Часть A (репозиторий публичный, простота важнее интеграции).

---

## 📋 Общие шаги (применяются к обоим вариантам)

### Шаг 1 — Отредактируй файл

Все материалы лежат в папке `docs\`:

```
docs\
  index.md                    ← главная страница сайта
  DARK_STORE_PROJECT_SUMMARY.md
  FINANCIAL_MODEL.md
  ROADMAP_12_MONTHS.md
  MVP_PLAN.md
  ... и т.д.
```

Открой нужный файл в любом редакторе (Rider, VS Code, Notepad++) и внеси правки.

> **⚠️ Важно:** сохраняй файлы в кодировке **UTF-8**. В Rider: File → File Encoding → UTF-8.

### Шаг 2 — Проверь локально (рекомендуется)

```powershell
cd C:\dev\darkstore
.\docs-serve.ps1
```

Откроется браузер на `http://127.0.0.1:8000` — смотришь как выглядит результат.
Файлы обновляются **автоматически** при сохранении — не нужно перезапускать.
Когда доволен — нажми `Ctrl+C` для остановки.

### Шаг 3 — Закоммить изменения

```powershell
cd C:\dev\darkstore
git add docs/FINANCIAL_MODEL.md           # конкретный файл
# или
git add docs/                              # все изменённые файлы в docs\
git status                                 # проверка
git commit -m "docs: обновлена финансовая модель — добавлен Q3 прогноз"
```

### Шаг 4 — Задеплой

- **A (GitHub Pages):** `python -m mkdocs gh-deploy --force`
- **B (Azure SWA):** `git push origin main` (workflow сделает остальное)

---

## 🗂️ Добавить новый раздел документации

### 1. Создай файл

```powershell
New-Item docs\ANALYTICS.md
```

Напиши содержимое в `docs\ANALYTICS.md`.

### 2. Добавь в навигацию

Открой `mkdocs.yml` и добавь страницу в нужный раздел:

```yaml
nav:
  # ... существующие разделы ...

  - 💰 Бизнес:
    - Финансовая модель: FINANCIAL_MODEL.md
    - Аналитика: ANALYTICS.md   # ← добавил сюда
```

### 3. Задеплой (см. Шаг 4 выше)

```powershell
git add docs/ANALYTICS.md mkdocs.yml
git commit -m "docs: добавлен раздел Аналитика"
# A:
python -m mkdocs gh-deploy --force
# или B:
git push origin main
```

---

## 🔧 Полезные команды

| Команда | Что делает |
|---|---|
| `.\docs-serve.ps1` | Локальный сервер на `localhost:8000` |
| `.\docs-serve.ps1 -Network` | Доступ с телефона по Wi-Fi (выводит QR) |
| `.\docs-serve.ps1 -Tunnel` | Публичный URL через Cloudflare (любой интернет) |
| `.\docs-serve.ps1 -Build` | Только собрать сайт в `site\`, без деплоя |
| `python -m mkdocs gh-deploy --force` | **A:** собрать + задеплоить на GitHub Pages |
| `git push origin main` | **B:** триггер автодеплоя через GitHub Actions → Azure SWA |
| `git log --oneline -10` | Последние 10 коммитов |
| `git diff docs/` | Посмотреть несохранённые изменения |

---

## ❓ Частые проблемы

### «На сайте отображается старая версия»

Браузер кеширует страницы. Нажми **Ctrl+Shift+R** (Windows) или **Cmd+Shift+R** (Mac) для принудительной перезагрузки.
Если не помогает — открой в режиме инкогнито.

### «git push требует логин/пароль»

GitHub больше не принимает пароли — нужен Personal Access Token (PAT).

1. Открой: [https://github.com/settings/tokens/new](https://github.com/settings/tokens/new)
2. Название: `darkstore-deploy`, отметь `repo`
3. Скопируй токен и используй его как пароль при запросе git

Или задай токен один раз:

```powershell
git remote set-url origin https://alexander-shamray:ВАШ_ТОКЕН@github.com/alexander-shamray/darkstore.git
```

### **A:** «Your current plan does not support GitHub Pages» (HTTP 422)

Репозиторий приватный, GitHub Pages на Free-плане только для публичных репо.
Варианты:
- Сделать репо публичным (Settings → General → Danger Zone → Change visibility)
- Перейти на платный план GitHub
- Использовать **Часть B (Azure SWA)**

### **A:** «ERROR — Config file not found»

Убедись что запускаешь команды из корня проекта `C:\dev\darkstore`, а не из подпапки.

```powershell
cd C:\dev\darkstore
python -m mkdocs gh-deploy --force
```

### **B:** «Deploy to Azure Static Web Apps — invalid token»

Секрет `AZURE_STATIC_WEB_APPS_API_TOKEN` не задан или истёк.

1. Azure Portal → твой ресурс SWA → Overview → "Manage deployment token" → Reset / Copy
2. GitHub → Settings → Secrets and variables → Actions → обнови `AZURE_STATIC_WEB_APPS_API_TOKEN`
3. Перезапусти упавший workflow run.

### **B:** «Workflow не запускается при push»

Workflow триггерится **только** на изменения в `docs/**` или `mkdocs.yml`. Если изменён только бэкенд — деплой документации не нужен. Чтобы задеплоить вручную: GitHub → Actions → Deploy Docs — Azure Static Web Apps → Run workflow.

### «UnicodeDecodeError при сборке»

Один из `.md`-файлов сохранён не в UTF-8. Исправление:

```powershell
cd C:\dev\darkstore
python -c "
import os
for f in os.listdir('docs'):
    if not f.endswith('.md'): continue
    for enc in ['utf-8-sig','utf-8','cp1251','latin-1']:
        try:
            txt = open('docs/'+f, encoding=enc).read()
            open('docs/'+f, 'w', encoding='utf-8').write(txt)
            break
        except: pass
print('done')
"
```

---

## 📌 Структура проекта (справочно)

```
C:\dev\darkstore\
│
├── mkdocs.yml                           ← конфигурация сайта (навигация, тема, плагины)
│
├── docs\                                ← ВСЕ исходные .md файлы документации
│   ├── index.md                         ← главная страница
│   ├── stylesheets\
│   │   └── extra.css                    ← кастомные стили (цвета)
│   └── *.md                             ← остальные документы
│
├── site\                                ← собранный статический сайт (НЕ редактировать)
│   └── ...                              ← генерируется автоматически, в git не хранится
│
├── .github\workflows\
│   └── deploy-docs-swa.yml              ← (только для Часть B) workflow деплоя в Azure SWA
│
└── docs-serve.ps1                       ← скрипт для локального просмотра и туннеля
```
