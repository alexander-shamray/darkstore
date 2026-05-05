# 📘 Технический индекс — Dark Store

> Порядок чтения для разработчика, входящего в проект.  
> Читай последовательно — каждый документ опирается на предыдущий.

---

## 🚀 Шаг 1 — Понять проект (5 мин)

| Документ | Что даёт |
|---------|---------|
| [DARK_STORE_PROJECT_SUMMARY.md](./DARK_STORE_PROJECT_SUMMARY.md) | Цель, УТП, архитектура, этапы — одна страница обо всём |
| [COMPETITOR_ANALYSIS.md](./COMPETITOR_ANALYSIS.md) | Матрица конкурентов (Glovo, Wolt, Kaspi, местные), конкурентные преимущества |
| [SOLUTION_COMPARISON.md](./SOLUTION_COMPARISON.md) | Почему выбрана собственная разработка: сравнение с готовыми платформами (Арзан ALEM), ставки команды ($50/ч архитектор, 4 500 тг/ч дизайнер КЗ), opportunity cost, franchise-риски |

---

## 🏗️ Шаг 2 — Понять архитектуру и стек (20 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 2.1 | [TECH_STACK.md](./TECH_STACK.md) | Полный стек: .NET 10, Angular, EF Core + Dapper, Hangfire, SignalR, гибридная БД; обоснование каждого выбора |
| 2.2 | [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md) | Схема всех таблиц, разделение 🇰🇿 KZ Local / ☁️ Azure, BaseEntity, пример оркестрации двух DbContext |
| 2.3 | [AUTH_AND_IDENTITY.md](./AUTH_AND_IDENTITY.md) | Система аутентификации и авторизации: Phone OTP + JWT, Social Login (Google, Apple, Microsoft, Meta), 2FA (TOTP + SMS), семейные подписки с общим пулом бонусов, регистрация по приглашению — 5 этапов без breaking changes БД |
| 2.4 | [LOGGING_AND_AUDIT_STRATEGY.md](./LOGGING_AND_AUDIT_STRATEGY.md) | Serilog конфигурация, уровни логов, MediatR LoggingBehavior + AuditBehavior, схема AuditLogs, защита ПДн в логах (Закон РК №94-V), Application Insights, алерты, политика хранения |

---

## 🧪 Шаг 2.5 — Стратегия тестирования и TDD (15 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 2.5 | [TESTING_STRATEGY.md](./TESTING_STRATEGY.md) | Пирамида тестирования, **TDD (Red→Green→Refactor)** с примерами на C# и TypeScript, Unit/Integration/Architecture/E2E тесты, Quality Gates, Chaos Testing |

> **TDD-правило:** PR на изменение доменной логики без тестов не принимается — блокируется Quality Gate в `deploy-azure1.yml`.  
> Краткий путь: `TESTING_STRATEGY.md` → раздел **[TDD — разработка через тестирование](#tdd)**

---

## 🛠️ Шаг 3 — Настроить проект с нуля (30 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 3.1 | [PROJECT_CREATION_STEPS.md](./PROJECT_CREATION_STEPS.md) | Пошагово: GitHub репо → Rider → Clean Architecture → NuGet → CI/CD → OIDC → API-версионирование |
| 3.2 | [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md) | Все переменные окружения, connection strings, secrets, SMS OTP провайдер, Firebase, Azure Key Vault; чеклист перед запуском |
| 3.3 | [BRAND_GUIDELINES.md](./BRAND_GUIDELINES.md) | Цвета, типографика, тон общения, именование в коде — единый стиль для всей команды |

---

## 🔗 Шаг 4 — Понять внешние зависимости (15 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 4.1 | [INTEGRATIONS.md](./INTEGRATIONS.md) | Все интеграции: 1С, Kaspi Pay, АЗС, Google Maps, Firebase Web Push — протоколы, статусы, библиотеки |

---

## 🚢 Шаг 5 — CI/CD и деплой (10 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 5.1 | [deploy-azure1.yml](./deploy-azure1.yml) | PR Check workflow — сборка + тесты + CVE-сканирование на каждый Pull Request |
| 5.2 | [deploy-azure2.yml](./deploy-azure2.yml) | Деплой: build → staging (авто) → production (ручное подтверждение); OIDC, health check, Telegram; retention 30 дней |
| 5.3 | [.github/workflows/rollback.yml](./.github/workflows/rollback.yml) | Откат production/staging на любой предыдущий SHA с Telegram-уведомлением (`workflow_dispatch`) |

---

## 📅 Шаг 6 — Что и когда делать (10 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 6.1 | [ROADMAP_12_MONTHS.md](./ROADMAP_12_MONTHS.md) | Квартальная дорожная карта Q1–Q4, ключевые вехи, приоритеты |

---

## ⚠️ Шаг 7 — Риски и что с ними делать (10 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 7.1 | [RISKS_AND_MITIGATION.md](./RISKS_AND_MITIGATION.md) | 15 рисков с вероятностью, влиянием и стратегией снижения (в т.ч. KZ Local DB SPOF, ОФД/ККМ, payment fraud) |
| 7.2 | [LEGAL_REQUIREMENTS.md](./LEGAL_REQUIREMENTS.md) | ОФД/ККМ, юрлицо (ИП/ТОО), Закон о ПДн, трудоустройство курьеров, СанПиН, чеклист перед запуском |

---

## 🔍 Справочно — текущий статус улучшений

| Документ | Что даёт |
|---------|---------|
| [ANALYSIS_AND_CHANGES.md](./ANALYSIS_AND_CHANGES.md) | Лог всех исправлений + критические открытые задачи, блокирующие запуск |
| [IMPROVEMENT_PLAN.md](./IMPROVEMENT_PLAN.md) | Полный план улучшений по 9 разделам (30+ задач с приоритетами и усилием) |
| [MANUAL_ACTIONS.md](./MANUAL_ACTIONS.md) | Чеклист всех ручных действий: юрист, Azure, GitHub, интеграции, операции — с критическим путём |
| [LOGGING_AND_AUDIT_STRATEGY.md](./LOGGING_AND_AUDIT_STRATEGY.md) | Комплексная стратегия логирования и аудита: Serilog, MediatR behaviors, AuditLogs, ПДн-защита, мониторинг |

---

## Быстрый старт — минимальный путь до первого коммита

```
DARK_STORE_PROJECT_SUMMARY  →  TECH_STACK  →  PROJECT_CREATION_STEPS  →  CONFIGURATION_GUIDE
```

## Быстрый старт — понять что именно строить

```
DATABASE_SCHEMA  →  AUTH_AND_IDENTITY  →  LOGGING_AND_AUDIT_STRATEGY  →  INTEGRATIONS  →  ROADMAP_12_MONTHS
```

## Быстрый старт — запустить локальную среду разработки

```bash
docker-compose up -d        # поднять SQL Server ×2 + Redis локально
dotnet restore              # восстановить NuGet-пакеты (global.json фиксирует SDK)
dotnet run --project src/DarkStore.API   # запустить API
# /scalar/v1  — интерактивный OpenAPI UI
# /hangfire   — Hangfire Dashboard (Admin только)
# /health/ready — health check
```

---

*Обновлён: Май 2026 — добавлены COMPETITOR_ANALYSIS, BRAND_GUIDELINES, LEGAL_REQUIREMENTS, IMPROVEMENT_PLAN, MANUAL_ACTIONS, rollback.yml, LOGGING_AND_AUDIT_STRATEGY, **AUTH_AND_IDENTITY**, **SOLUTION_COMPARISON**; обновлены CI/CD (CodeQL SAST + npm audit + dotnet format gate), стек (async Serilog, JWT + OAuth 2.0/OIDC + 2FA + семейные подписки, Rate Limiting, HSTS, Security Headers, Output Cache, Response Compression, OpenTelemetry, @ngrx/signals, @angular/google-maps, azure-key-vault); добавлены Directory.Build.props, global.json, docker-compose.yml*

