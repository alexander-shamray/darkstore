# 📤 Как обновить сайт документации

> **Сайт:** [https://alexander-shamray.github.io/darkstore/](https://alexander-shamray.github.io/darkstore/)  
> **Репозиторий:** [https://github.com/alexander-shamray/darkstore](https://github.com/alexander-shamray/darkstore)  
> **Время деплоя:** ~30–60 секунд после команды

---

## ⚡ Быстро — одна команда

Если просто отредактировал `.md`-файл и хочешь опубликовать:

```powershell
cd C:\dev\darkstore
git add docs/
git commit -m "docs: описание что изменил"
python -m mkdocs gh-deploy --force
```

Готово. Через ~30 секунд изменения появятся на сайте.

---

## 📋 Полный цикл по шагам

### Шаг 1 — Отредактируй файл

Все материалы лежат в папке `docs\`:

```
docs\
  index.md                    ← главная страница сайта
  DARK_STORE_PROJECT_SUMMARY.md
  FINANCIAL_MODEL.md
  ROADMAP_12_MONTHS.md
  ... и т.д.
```

Открой нужный файл в любом редакторе (Rider, VS Code, Notepad++) и внеси правки.

> **⚠️ Важно:** сохраняй файлы в кодировке **UTF-8**. В Rider: File → File Encoding → UTF-8.

---

### Шаг 2 — Проверь локально (необязательно, но рекомендуется)

```powershell
cd C:\dev\darkstore
.\docs-serve.ps1
```

Откроется браузер на `http://127.0.0.1:8000` — смотришь как выглядит результат.  
Файлы обновляются **автоматически** при сохранении — не нужно перезапускать.  
Когда доволен — нажми `Ctrl+C` для остановки.

---

### Шаг 3 — Закоммить изменения

```powershell
cd C:\dev\darkstore

# Добавить конкретный файл
git add docs/FINANCIAL_MODEL.md

# Или все изменённые файлы в docs\
git add docs/

# Посмотреть что будет закоммичено
git status

# Создать коммит с описанием
git commit -m "docs: обновлена финансовая модель — добавлен Q3 прогноз"
```

---

### Шаг 4 — Опубликовать на сайт

```powershell
python -m mkdocs gh-deploy --force
```

Команда:
1. Собирает статический сайт в папку `site\`
2. Пушит его в ветку `gh-pages` на GitHub
3. GitHub Pages публикует автоматически

Ожидаемый вывод:
```
INFO  - Documentation built in 1.4 seconds
INFO  - Copying 'site' to 'gh-pages' branch and pushing to GitHub.
INFO  - Your documentation should shortly be available at: https://alexander-shamray.github.io/darkstore/
```

---

### Шаг 5 — Опционально: запушить исходники в main

Если хочешь сохранить исходные `.md`-файлы в репозитории (рекомендуется):

```powershell
git push origin main
```

---

## 🗂️ Добавить новый раздел документации

### 1. Создай файл

```powershell
# Пример: новый документ про аналитику
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

### 3. Задеплой (те же команды из шага 3–4 выше)

```powershell
git add docs/ANALYTICS.md mkdocs.yml
git commit -m "docs: добавлен раздел Аналитика"
python -m mkdocs gh-deploy --force
```

---

## 🔧 Полезные команды

| Команда | Что делает |
|---|---|
| `.\docs-serve.ps1` | Локальный сервер на `localhost:8000` |
| `.\docs-serve.ps1 -Network` | Доступ с телефона по Wi-Fi (выводит QR) |
| `.\docs-serve.ps1 -Tunnel` | Публичный URL через Cloudflare (любой интернет) |
| `.\docs-serve.ps1 -Build` | Только собрать сайт в `site\`, без деплоя |
| `python -m mkdocs gh-deploy --force` | Собрать + задеплоить на GitHub Pages |
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

### «ERROR - Config file not found»

Убедись что запускаешь команды из корня проекта `C:\dev\darkstore`, а не из подпапки.

```powershell
cd C:\dev\darkstore
python -m mkdocs gh-deploy --force
```

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
├── mkdocs.yml              ← конфигурация сайта (навигация, тема, плагины)
│
├── docs\                   ← ВСЕ исходные .md файлы документации
│   ├── index.md            ← главная страница
│   ├── stylesheets\
│   │   └── extra.css       ← кастомные стили (цвета)
│   └── *.md                ← остальные документы
│
├── site\                   ← собранный статический сайт (НЕ редактировать)
│   └── ...                 ← генерируется автоматически, в git не хранится
│
└── docs-serve.ps1          ← скрипт для локального просмотра и туннеля
```

> Ветка `main` → исходники (`docs\`, `mkdocs.yml`)  
> Ветка `gh-pages` → собранный сайт (обновляется командой `gh-deploy`)

