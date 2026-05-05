# Пошаговое создание проекта Dark Store

**Цель:** Создать с нуля современный проект на .NET 10 + Angular с использованием Clean Architecture, GitHub Actions и деплоя в Azure App Service.

---

## Шаг 1: Подготовка аккаунтов

1. Создай аккаунт на **GitHub** (если ещё нет)
2. Создай бесплатную подписку в **Azure Portal**
3. Убедись, что у тебя установлен **JetBrains Rider 2026**
4. Создай **Fine-grained Personal Access Token** в GitHub со следующими правами:
   - `Contents`: Read and write
   - `Actions`: Read and write
   - `Pull requests`: Read and write
   - `Workflows`: Read and write
   - `Metadata`: Read-only

> 🛡️ **Fault Tolerance — Шаг 1:**
> - Включи **2FA (двухфакторную аутентификацию)** на GitHub и Azure — единственная точка отказа при взломе аккаунта.
> - Сохрани **Recovery Codes** от 2FA в защищённом офлайн-хранилище (не в браузере).
> - Создай **два PAT-токена** с разными сроками действия — один рабочий, второй резервный. При компрометации одного немедленно отзови и замени.
> - В Azure Portal настрой **резервную контактную почту** и **телефон** для восстановления аккаунта.
> - Для Azure: включи **Azure Cost Alert** ($20/мес порог) — защита от непредвиденных расходов при утечке ключей.
> - Задокументируй все аккаунты / сервисы в менеджере паролей (1Password / Bitwarden), доступном без подключения к сети.

---

## Шаг 2: Создание репозитория на GitHub

1. Зайди на GitHub и нажми **New repository**
2. Заполни данные:
   - Repository name: `darkstore-backend`
   - Visibility: **Private**
   - Не добавляй README и `.gitignore` (сделаем позже)
3. Нажми **Create repository**
4. Скопируй HTTPS-ссылку репозитория

> 🛡️ **Fault Tolerance — Шаг 2:**
> - Настрой **второй remote** (`git remote add backup <другой_git_хостинг>`) — например, GitLab или собственный Gitea. Push в оба репозитория после каждого спринта.
> - Включи функцию **"Keep my email private"** в GitHub (Settings → Emails), чтобы исключить спам/фишинг.
> - Создай защищённую **`main` ветку** немедленно (даже если единственный разработчик) — случайный `git push --force` в main без защиты уничтожит историю.
> - Заранее добавь в `.gitignore` шаблон .NET (через `gitignore.io`), чтобы исключить случайный коммит секретов.
> - Сделай **локальный бэкап репозитория** раз в неделю: `git bundle create darkstore-backup-$(date +%Y%m%d).bundle --all` и сохрани на внешний диск.

---

## Шаг 3: Создание решения в Rider

1. Открой **Rider**
2. Нажми **New Solution**
3. Выбери шаблон: **ASP.NET Core Web API**
4. Укажи:
   - .NET version: **.NET 10.0**
   - Solution name: `DarkStore`
   - Project name: `DarkStore.API`
5. Создай решение

> 🛡️ **Fault Tolerance — Шаг 3:**
> - Сразу после создания решения добавь файл `.editorconfig` для единообразия кода.
> - Включи в `DarkStore.API.csproj` свойство `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — предупреждения компилятора в CI превращаются в ошибки сборки.
> - Добавь `<Nullable>enable</Nullable>` — защита от `NullReferenceException` на уровне компилятора.
> - Включи `<ImplicitUsings>enable</ImplicitUsings>` с явными using для критичных пространств имён, чтобы исключить случайные конфликты имён.
> - Сразу создай `global.json` с фиксацией версии SDK: `{ "sdk": { "version": "10.0.xxx", "rollForward": "patch" } }` — защита от непредвиденных изменений при обновлении SDK.

---

## Шаг 4: Базовая структура проекта (Clean Architecture)

Создай следующую структуру папок внутри решения:

DarkStore/
├── src/
│   ├── DarkStore.Domain
│   ├── DarkStore.Application
│   ├── DarkStore.Infrastructure
│   └── DarkStore.API
├── tests/
│   └── DarkStore.Tests          ← Unit/Integration тесты
└── .github/
    └── workflows/


**Установи базовые NuGet-пакеты:**

```bash
# CQRS + Validation + Mapping
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package Mapster
dotnet add package Mapster.DependencyInjection

# ORM: EF Core (writes) + Dapper (reads)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Dapper

# OpenAPI — Scalar вместо Swashbuckle
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Scalar.AspNetCore

# Auth + Logging + Versioning
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Serilog.AspNetCore
dotnet add package Asp.Versioning.Http

# Фоновые задачи
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer

# Real-time (SignalR встроен в ASP.NET Core — пакет не нужен)
```

**Создай тестовый проект:**

```bash
dotnet new xunit -n DarkStore.Tests -o tests/DarkStore.Tests
dotnet add tests/DarkStore.Tests/DarkStore.Tests.csproj reference src/DarkStore.Application/DarkStore.Application.csproj
dotnet add tests/DarkStore.Tests package Moq
dotnet add tests/DarkStore.Tests package FluentAssertions
```

> 🛡️ **Fault Tolerance — Шаг 4:**
>
> **Пакеты устойчивости (обязательно добавить сразу):**
> ```bash
> # Resilience: retry, circuit breaker, timeout для всех HttpClient
> dotnet add package Microsoft.Extensions.Http.Resilience
>
> # Health checks: база данных, Redis, внешние сервисы
> dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
> dotnet add package AspNetCore.HealthChecks.SqlServer
> dotnet add package AspNetCore.HealthChecks.Redis
> dotnet add package AspNetCore.HealthChecks.Uris
>
> # Мониторинг
> dotnet add package Microsoft.ApplicationInsights.AspNetCore
> ```
>
> **Архитектурные решения устойчивости:**
> - **Outbox Pattern** — сразу добавь таблицу `OutboxMessages` и Hangfire-джоб обработки; защищает от потери событий при сбое внешних сервисов.
> - **Idempotency Keys** — для всех write-операций API добавь поддержку `Idempotency-Key` header (uuid4); повторный запрос с тем же ключом возвращает закешированный ответ, не создавая дубли.
> - **Domain Events** через Outbox — смена статуса заказа НЕ вызывает внешние сервисы синхронно; события публикуются в `OutboxMessages` и обрабатываются Hangfire.
> - **Optimistic Concurrency** — добавь `rowversion` / `ConcurrencyToken` в EF Core конфигурации критичных сущностей (`Orders`, `Inventory`); защита от race condition при параллельных запросах.
> - **Circuit Breaker State Machine** — при старте приложения инициализируй счётчики сбоев для каждого внешнего сервиса (1С, Kaspi Pay, SMS, AZS) независимо.
> - **Graceful Degradation** — если KZ Local DB недоступна, API должен возвращать `503 Service Unavailable` с `Retry-After` заголовком, а не `500 Internal Server Error`.

## Шаг 5: Первый коммит и push в GitHub

Выполни в терминале Rider:
```bash
git init
git add .
git commit -m "Initial commit: Project structure"
git branch -M main
git remote add origin <вставь_ссылку_на_репозиторий>
git push -u origin main
```

> 🛡️ **Fault Tolerance — Шаг 5:**
> - Перед первым push добавь `.gitignore` (шаблон .NET от GitHub/gitignore.io) — один коммит секретов в историю требует полного удаления через `git filter-repo`, что разрушительно.
> - Добавь **pre-commit hook** через `dotnet-format` или Husky (если Frontend): автоформатирование кода перед коммитом.
> - Настрой **GitHub Secret Scanning** (Settings → Security → Secret scanning): GitHub автоматически выявляет случайно добавленные ключи и уведомляет.
> - Добавь **Dependabot** (`/.github/dependabot.yml`) сразу — ежеслучайное уведомление об уязвимых пакетах критично для безопасности с первого дня.
> - После push немедленно задай **Branch Protection** (см. Шаг 8.6) — нельзя откладывать.

## Шаг 6: Настройка GitHub Actions (CI/CD)

В репозитории уже подготовлены два pipeline-файла — **не создавай новый вручную**, просто скопируй из репозитория:

| Файл | Назначение |
|------|-----------|
| `.github/workflows/deploy-azure1.yml` | PR Check — сборка + тесты + покрытие кода (без деплоя) |
| `.github/workflows/deploy-azure2.yml` | Main pipeline — staging (авто) → production (ручное подтверждение) |

> ⚠️ **Важно:** оба файла используют `.NET 10.0.x` и OIDC-аутентификацию (без Publish Profile). Настройка OIDC описана в **Шаге 8**.

Как настроить уведомления в Telegram

Создай бота
Открой Telegram и найди бота @BotFather
Отправь команду /newbot
Придумай имя бота (например: DarkStoreDeployBot)
Скопируй API Token (он выглядит как 123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11)

Получи Chat ID
Добавь своего бота в личные сообщения или в группу
Отправь боту любое сообщение
Зайди по ссылке: https://api.telegram.org/bot<ТОКЕН_БОТА>/getUpdates
Найди поле "chat":{"id": ...} — это и есть твой TELEGRAM_CHAT_ID

Добавь секреты в GitHub
Перейди в репозиторий → Settings → Secrets and variables → Actions
Добавь два новых секрета:

Secret Name,Значение
TELEGRAM_BOT_TOKEN,Токен бота от @BotFather
TELEGRAM_CHAT_ID,"Chat ID (число, например -1001234567890)"

> 🛡️ **Fault Tolerance — Шаг 6:**
>
> **Устойчивость самого CI/CD pipeline:**
> - Добавь `timeout-minutes: 20` ко всем job — предотвращает бесконечное зависание при сбое тестов или сетевых проблемах.
> - Используй `continue-on-error: false` для критичных steps (тесты, build) и `continue-on-error: true` для некритичных (coverage badge update).
> - Настрой **retry для flaky-тестов**: в CI используй `retry: 2` для integration-тестов (Testcontainers иногда падает при первом Docker pull).
> - Добавь **artifact retention** (`retention-days: 30`) — возможность откатить деплой на любую сборку последнего месяца.
> - Добавь **dependency caching** для NuGet и npm:
>   ```yaml
>   - uses: actions/cache@v4
>     with:
>       path: ~/.nuget/packages
>       key: nuget-${{ hashFiles('**/*.csproj') }}
>       restore-keys: nuget-
>   ```
>   Без кэша каждый запуск скачивает пакеты заново — если NuGet.org недоступен, CI сломается.
> - Настрой **GitHub Actions Concurrency** для предотвращения параллельных деплоев одного окружения:
>   ```yaml
>   concurrency:
>     group: deploy-${{ github.ref }}
>     cancel-in-progress: false  # не отменять — ждать завершения
>   ```
> - Зафиксируй версии actions через SHA-хэш (не `@v4`, а `@a1b2c3...`) — защита от supply-chain атак.
> - Telegram-уведомления должны срабатывать как при **успехе**, так и при **провале** — иначе silent failure остаётся незамеченным.


## Шаг 7: Создание Azure App Service

Зайди в Azure Portal
Создай ресурс App Service
Выбери:
Runtime stack: .NET 10 (custom / preview bin; или .NET 9 LTS если .NET 10 ещё не в GA для App Service)
Operating System: Linux (рекомендуется)
Pricing tier: Basic B1 (на старте)

После создания перейди в Deployment Center → Get publish profile
Скачай файл .PublishSettings

> 🛡️ **Fault Tolerance — Шаг 7:**
> - **Deployment Slots** — обязательно создай `staging` слот. Деплой всегда идёт в staging → smoke tests → `swap`. При сбое swap доступен мгновенный rollback без нового деплоя (`az webapp deployment slot swap --slot staging --target-slot production`).
> - **Health Check Endpoint** — сразу настрой `GET /health` эндпоинт (реагирует за < 200ms). В Azure Portal → App Service → Health check укажи `/health` и минимальный порог `2 — из 10 успешных`. Azure автоматически перезапустит instance при провале.
> - **Always On** — включи (Settings → Configuration → General → Always On: On) для предотвращения cold start. Без этого первый запрос после простоя занимает 10–30 секунд.
> - **Auto-heal** — настрой правила автоматического перезапуска (Settings → Diagnose and solve → Auto Heal):
>   - Memory usage > 90% → restart
>   - HTTP 5xx errors > 20/min → restart
>   - Request duration > 30 sec → restart
> - **Connection Resilience** в `appsettings.json`:
>   ```json
>   "ConnectionResiliency": {
>     "MaxRetryCount": 5,
>     "MaxRetryDelay": "00:00:30"
>   }
>   ```
>   В EF Core: `.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)` — защита от transient SQL failures.
> - **Scaling** — даже на B1 настрой вертикальное масштабирование до P2V3 в случае инцидента: правило `CPU > 80% за 10 мин → scale up`. Откат по расписанию (ночью).
> - **App Service Plan Redundancy** — на B1 нет SLA. Для prod рекомендуется S1+ (99.95% SLA). Зафиксируй план перехода (при > 50 заказов/день выполни upgrade до S1).



OIDC (Federated Identity Credential) — современный и безопасный способ. Не требует статических секретов (Publish Profile), токен запрашивается автоматически при каждом запуске.

### 8.1. Создание App Registration в Azure AD

```bash
# Через Azure CLI (или вручную в Azure Portal)

# Войти в Azure
az login

# Создать App Registration
az ad app create --display-name "darkstore-github-actions"

# Запомни вывод: "appId" — это AZURE_CLIENT_ID
```

Или через портал:
1. **Azure Portal → Microsoft Entra ID → App registrations → New registration**
2. Имя: `darkstore-github-actions`, тип: Single tenant
3. Скопируй **Application (client) ID** → это `AZURE_CLIENT_ID`
4. Скопируй **Directory (tenant) ID** → это `AZURE_TENANT_ID`

---

### 8.2. Добавление Federated Identity Credential

```bash
# Получи ID созданного приложения
APP_ID=$(az ad app list --display-name "darkstore-github-actions" --query "[0].appId" -o tsv)

# Добавить Federated Credential для GitHub Actions (ветка main)
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "darkstore-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<ТВОЙ_GITHUB_USERNAME>/darkstore-backend:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

Или через портал:
1. **App Registration → Certificates & secrets → Federated credentials → Add credential**
2. Scenario: **GitHub Actions deploying Azure resources**
3. Заполни:
   - Organization: `<твой GitHub username>`
   - Repository: `darkstore-backend`
   - Entity type: **Branch**
   - Branch: `main`
   - Name: `darkstore-main`

---

### 8.3. Создание Service Principal и назначение роли

```bash
# Создать Service Principal для приложения
az ad sp create --id $APP_ID

# Получи Subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Назначить роль "Contributor" на уровне Resource Group
az role assignment create \
  --assignee $APP_ID \
  --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/darkstore-rg"
```

> 💡 Используй **Resource Group scope**, а не Subscription scope — меньше прав, больше безопасность.

> 🛡️ **Fault Tolerance — 8.3:**
> - Принцип **Least Privilege**: выдавай роль `Contributor` только на конкретный Resource Group, не на подписку.
> - Периодически ротируй App Registration — настрой Calendar-reminder каждые 12 месяцев на проверку и обновление Federated Credentials.
> - Если OIDC перестал работать (GitHub изменил формат subject), добавь **второй Federated Credential** на environment `staging` для независимого деплоя staging без prod-credential.

---

### 8.4. Добавление GitHub Secrets

В репозитории: **Settings → Secrets and variables → Actions → New repository secret**

| Secret Name | Значение | Где взять |
|------------|---------|----------|
| `AZURE_CLIENT_ID` | Application (client) ID | App Registration → Overview |
| `AZURE_TENANT_ID` | Directory (tenant) ID | App Registration → Overview |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID | Azure Portal → Subscriptions |
| `AZURE_WEBAPP_NAME` | Имя Production App Service | Azure Portal → App Services |
| `AZURE_WEBAPP_NAME_STAGING` | Имя Staging App Service | Azure Portal → App Services |
| `TELEGRAM_BOT_TOKEN` | Токен бота от @BotFather | Telegram |
| `TELEGRAM_CHAT_ID` | Chat ID для уведомлений | getUpdates API |

> ❌ `AZURE_WEBAPP_PUBLISH_PROFILE` и `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` больше **не нужны**.

> 🛡️ **Fault Tolerance — 8.4:**
> - Документируй смысл каждого секрета в Notion/README-protected — при ротации секрета нельзя угадать, куда его вставить.
> - Настрой **GitHub Secret expiry reminder**: добавь в calendar напоминание каждые 6 месяцев для ревью и ротации секретов.
> - Если `AZURE_CLIENT_ID` скомпрометирован — немедленный план: удалить App Registration, создать новый, обновить все GitHub Secrets и перезапустить деплой.

---

### 8.5. Настройка GitHub Environments (ручное подтверждение Production)

1. Репозиторий → **Settings → Environments → New environment**
2. Создай `staging` — без ограничений (деплой автоматический)
3. Создай `production`:
   - Включить **Required reviewers** → добавить себя
   - Деплой в Production потребует нажатия **"Approve"** в GitHub Actions

> 🛡️ **Fault Tolerance — 8.5:**
> - Добавь **Wait timer: 5 минут** для `production` environment — окно для автоматических smoke-тестов на staging до одобрения.
> - Настрой **Deployment protection rule**: "Required reviewers" блокирует случайный авто-деплой в production при ошибке конфигурации pipeline.
> - Добавь **Environment secrets** (отдельно от Repository secrets) для prod-специфичных значений — изоляция конфигурации staging/prod.

---

### 8.6. Branch Protection Rules для ветки `main`

**Обязательно настроить до первого PR:**

1. Репозиторий → **Settings → Branches → Add branch protection rule**
2. Branch name pattern: `main`
3. Включить следующие опции:

| Опция | Настройка |
|-------|---------|
| **Require a pull request before merging** | ✅ Включить |
| — Required approvals | 1 (себя включить как reviewer) |
| — Dismiss stale reviews when new commits are pushed | ✅ |
| **Require status checks to pass before merging** | ✅ Включить |
| — Required checks | `build-and-test` (из `deploy-azure1.yml`) |
| **Require branches to be up to date before merging** | ✅ |
| **Do not allow bypassing the above settings** | ✅ (даже для admin) |
| **Restrict force pushes** | ✅ Запретить |
| **Restrict deletions** | ✅ Запретить |

> ⚡ **Критично:** без `Require status checks` разработчик может случайно влить PR с падающими тестами или уязвимыми пакетами прямо в production pipeline.

> 🛡️ **Fault Tolerance — 8.6:**
> - Добавь в Required checks: `build-and-test` **И** vulnerability scan (`dotnet list package --vulnerable`). Один небезопасный пакет в production = потенциальная компрометация данных клиентов.
> - Включи **"Restrict force pushes"** — случайный `git push --force` от нового collaborator может уничтожить историю коммитов.
> - Настрой **Protected tags** (`v*`) — предотвращает удаление или перемещение релизных тегов.

---

### 8.7. Rollback процедура (App Service Deployment Slots)

При использовании staging slot деплой выглядит так: код деплоится в staging → тесты → `swap` staging ↔ production.

**Быстрый rollback через swap:**
```bash
# Откатить production к предыдущей версии (staging содержит старый код)
az webapp deployment slot swap \
  --resource-group darkstore-rg \
  --name darkstore-api \
  --slot staging \
  --target-slot production
```

**Rollback через GitHub Actions (`workflow_dispatch`):**
```yaml
# .github/workflows/rollback.yml
on:
  workflow_dispatch:
    inputs:
      commit_sha:
        description: 'Commit SHA to rollback to'
        required: true
jobs:
  rollback:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ github.event.inputs.commit_sha }}
      - name: Deploy specific commit to production
        # ... деплой шаги
```

> 📌 **Retention артефактов:** установить 30 дней в `deploy-azure2.yml` (`retention-days: 30`) чтобы иметь возможность откатиться на любую версию последнего месяца.

> 🛡️ **Fault Tolerance — 8.7 (Расширенные сценарии Rollback):**
>
> **Сценарий A: Критический баг обнаружен после деплоя (< 5 мин)**
> ```bash
> # Немедленный rollback через slot swap (< 60 секунд)
> az webapp deployment slot swap \
>   --resource-group darkstore-rg --name darkstore-api \
>   --slot staging --target-slot production
> # Сообщить команде в Telegram о rollback
> ```
>
> **Сценарий B: Массовый сбой API после успешного деплоя (обнаружен через App Insights)**
> ```bash
> # Посмотреть текущую версию
> az webapp show --resource-group darkstore-rg --name darkstore-api --query "siteConfig.linuxFxVersion"
> # Перезапустить (если проблема transient)
> az webapp restart --resource-group darkstore-rg --name darkstore-api
> # Если не помогло — rollback через slot
> ```
>
> **Сценарий C: Database Migration поломала данные**
> - Немедленно откатить App Service (slot swap)
> - Запустить `dotnet ef database update <PreviousMigrationName>` на staging
> - Применить Point-in-Time restore БД (см. `OPERATIONS_MANUAL.md` §10.4)
>
> **Сценарий D: Полная недоступность Azure Region**
> - Переключить DNS на статическую страницу "Технические работы" (Azure CDN Static Page)
> - Уведомить клиентов через Telegram-канал
> - Ждать восстановления региона (обычно < 4 часов)
>
> **Runbook (хранить офлайн):** Распечатай эту процедуру и храни физически — при инциденте GitHub может быть недоступен.

## Шаг 9: Проверка работы CI/CD

Сделай любое небольшое изменение в коде и закоммить
Зайди во вкладку Actions и убедись, что workflow запустился
Проверь, что приложение успешно задеплоилось в Azure

> 🛡️ **Fault Tolerance — Шаг 9:**
> - После первого успешного деплоя сразу выполни **Smoke Tests** вручную:
>   - `GET /health` → 200 OK
>   - `GET /openapi/v1.json` → 200 OK
>   - `GET /api/v1/products` → 200 OK (не 500)
> - Запиши **базовый Response Time** первого деплоя (Postman / Rider HTTP client) — это baseline для будущих сравнений производительности.
> - Проверь App Insights: убедись, что телеметрия поступает (Dashboard → Live Metrics).
> - Сымитируй failure: временно поломай connection string → убедись, что Health Check возвращает `503`, а не `200`.
> - Убедись, что Telegram-бот уведомил об успешном деплое — если нет, Pipeline работает неправильно.


## Шаг 10: Следующие шаги после базовой настройки
После успешного деплоя рекомендуется сделать следующее:

1. Добавить Swagger в проект
2. Создать первую миграцию Entity Framework
3. Добавить Health Check endpoint (`/health`)
4. Создать базовую структуру с MediatR (первый Use Case)
5. Настроить API-версионирование (см. ниже)
6. Начать разработку Angular фронтенда (в папке `client/`)

> 🛡️ **Fault Tolerance — Шаг 10:**
>
> **Расширенный Health Check (обязательно перед публичным запуском):**
> ```csharp
> // Program.cs
> builder.Services.AddHealthChecks()
>     .AddSqlServer(config["ConnectionStrings:AzureConnection"],
>         name: "azure-sql", tags: ["db", "azure"])
>     .AddSqlServer(config["ConnectionStrings:KzLocalConnection"],
>         name: "kz-local-sql", tags: ["db", "kz"])
>     .AddRedis(config["ConnectionStrings:Redis"],
>         name: "redis", tags: ["cache"])
>     .AddUrlGroup(new Uri(config["OneCIntegration:BaseUrl"] + "/ping"),
>         name: "1c-integration", tags: ["external"]);
>
> // Отдельные endpoints: /health (все), /health/live (только жизнь), /health/ready (готовность)
> app.MapHealthChecks("/health");
> app.MapHealthChecks("/health/ready", new() {
>     Predicate = check => check.Tags.Contains("db")
> });
> app.MapHealthChecks("/health/live", new() {
>     Predicate = _ => false  // всегда OK если процесс жив
> });
> ```
>
> **Первая миграция — защита от потери данных:**
> - Всегда проверяй миграцию на Staging перед Production.
> - Добавь в migration `Down()` метод — без него rollback невозможен.
> - Перед каждой prod-миграцией создай manual backup (см. `OPERATIONS_MANUAL.md` §10.5).
>
> **MediatR Behaviors (Pipeline) — первыми реализуй:**
> 1. `ValidationBehavior` — FluentValidation перехватывает невалидные команды до Handler
> 2. `LoggingBehavior` — Serilog логирует каждый Command/Query с CorrelationId
> 3. `RetryBehavior` — для идемпотентных операций (Queries): повтор при transient DB errors
> 4. `TransactionBehavior` — EF Core transaction scope для всех Commands



---

## Шаг 11: Настройка API-версионирования

Пакет `Asp.Versioning.Http` уже установлен на Шаге 4. Добавь конфигурацию:

**`Program.cs`:**
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

**Пример контроллера:**
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() { /* ... */ }
}
```

**Swagger с версиями:**
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dark Store API",
        Version = "v1",
        Description = "Dark Store — API быстрой доставки продуктов"
    });
});
```

> 📌 Подробнее: стратегия версионирования и Sunset Policy описаны в `TECH_STACK.md`, раздел 2а.

> 🛡️ **Fault Tolerance — Шаг 11:**
> - **Backward-Compatible Changes Only** в v1 — добавление полей допустимо, удаление и переименование требует v2. Нарушение ломает mobile-клиенты без уведомления.
> - **Deprecation Headers** — при переходе на v2 добавь к v1 ответам:
>   ```csharp
>   Response.Headers.Add("Deprecation", "true");
>   Response.Headers.Add("Sunset", "Sat, 01 Nov 2026 00:00:00 GMT");
>   Response.Headers.Add("Link", "</api/v2/products>; rel=\"successor-version\"");
>   ```
> - **Version Fallthrough** — конфигурация `AssumeDefaultVersionWhenUnspecified = true` защищает от ошибок клиентов, не передающих версию; не убирай этот флаг.
> - **Contract Tests** — при добавлении v2 обязательно пиши Regression тесты для v1, чтобы убедиться, что v1 не сломан одновременно.


