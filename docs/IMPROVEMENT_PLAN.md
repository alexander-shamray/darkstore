# 📋 План улучшений — Dark Store

**Версия:** 1.0  
**Создан:** Май 2026  
**Статус:** В работе — MVP блокирующие улучшения реализованы

---

## Содержание

1. [Backend (.NET 10)](#1-backend-net-10)
2. [Frontend (Angular)](#2-frontend-angular)
3. [База данных](#3-база-данных)
4. [CI/CD](#4-cicd)
5. [Мониторинг и Observability](#5-мониторинг-и-observability)
6. [Надёжность интеграций](#6-надёжность-интеграций)
7. [Безопасность](#7-безопасность)
8. [Производительность](#8-производительность)
9. [Developer Experience](#9-developer-experience)

---

## 1. Backend (.NET 10)

### ✅ Реализовано (Май 2026)

| # | Улучшение | Файл | Приоритет |
|---|-----------|------|-----------|
| 1.1 | `DarkStore.Application` проект — CQRS-слой (MediatR handlers, FluentValidation, Mapster) | `src/DarkStore.Application/` | 🔴 MVP |
| 1.2 | `MediatR` 12.4.1 + pipeline behaviors (LoggingBehavior, ValidationBehavior, AuditBehavior) | `.csproj` / `Application/DI.cs` | 🔴 MVP |
| 1.3 | `FluentValidation.AspNetCore` 11.3.0 | `.csproj` | 🔴 MVP |
| 1.4 | `Mapster` 7.4.0 + `Mapster.DependencyInjection` — source generators, нулевые аллокации | `.csproj` | 🟡 Q1 |
| 1.5 | `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` — замена Swashbuckle | `Program.cs` | 🟡 Q1 |
| 1.6 | `Asp.Versioning.Http` 8.1.0 + `Asp.Versioning.Mvc.ApiExplorer` — `/api/v1/` | `Program.cs` | 🟡 Q1 |
| 1.7 | `Hangfire.AspNetCore` + `Hangfire.SqlServer` 1.8.14 — фоновые задачи | `Program.cs` / `.csproj` | 🔴 MVP |
| 1.8 | `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.7 — JWT авторизация | `Program.cs` | 🔴 MVP |
| 1.9 | Serilog async sinks (`WriteTo.Async()`) — исправление документированного анти-паттерна | `Program.cs` | 🔴 MVP |
| 1.10 | `Destructurama.Attributed` — `[NotLogged]`/`[LogMasked]` для защиты ПДн в логах | `Program.cs` / `.csproj` | 🔴 MVP |
| 1.11 | Serilog-обогатители: `WithMachineName`, `WithEnvironmentName`, `WithThreadId`, `WithProcessId` | `Program.cs` | 🟡 Q1 |
| 1.12 | `Azure.Extensions.AspNetCore.Configuration.Secrets` + `Azure.Identity` — Key Vault | `Program.cs` / `.csproj` | 🔴 MVP |
| 1.13 | `OpenTelemetry` + OTLP-экспортер (параллельный sink для будущего Grafana) | `Program.cs` | 🟢 Q3 |
| 1.A | Создать проект `DarkStore.Application` — CQRS-слой с MediatR handlers + pipeline behaviors (Logging/Validation/Audit) + FluentValidation validators + Mapster + `IAppDbContext` абстракция | `src/DarkStore.Application/` → `DependencyInjection.cs`, `Common/Behaviors/`, `Orders/Commands/`, `Orders/Queries/` | 🔴 MVP |
| 1.B | `ICurrentUserService` — извлечение UserId/Roles из JWT клеймов; интерфейс в Application, реализация `CurrentUserService` в DarkStore.API через `IHttpContextAccessor` | `Application/Common/Interfaces/ICurrentUserService.cs`, `API/Services/CurrentUserService.cs` | 🔴 MVP |

### 🔲 Ожидает реализации

| # | Улучшение | Приоритет |
|---|-----------|-----------|
| 1.C | `Mediator` (source gen) — рассмотреть как замену MediatR в Q3+ для производительности; см. комментарий в `DarkStore.Application.csproj` | 🟢 Q3 |

---

## 2. Frontend (Angular)

### 🔲 Ожидает реализации (Frontend ещё не создан)

| # | Улучшение | Пакет / Команда | Приоритет |
|---|-----------|----------------|-----------|
| 2.1 | Создать Angular workspace | `ng new darkstore-frontend --standalone --routing --style=scss` | 🔴 MVP |
| 2.2 | Tailwind CSS 4.x | `ng add @angular/tailwind` | 🔴 MVP |
| 2.3 | PrimeNG (UI-компоненты) | `npm install primeng` | 🔴 MVP |
| 2.4 | **`@ngrx/signals`** (NgRx Signal Store) — вместо классического NgRx Slice | `npm install @ngrx/signals` | 🟡 Q1 |
| 2.5 | `@microsoft/signalr` — OrderHub real-time | `npm install @microsoft/signalr` | 🔴 MVP |
| 2.6 | `@angular/pwa` — Service Worker + offline + Web Push | `ng add @angular/pwa` | 🟡 Q2 |
| 2.7 | `firebase` + `@angular/fire` — FCM токены | `npm install firebase @angular/fire` | 🟡 Q3 |
| 2.8 | `@angular/google-maps` — курьерская карта | `npm install @angular/google-maps` | 🟡 Q2 |
| 2.9 | **Jest** + `jest-preset-angular` — замена Karma | `npm install -D jest jest-preset-angular` | 🔴 MVP |
| 2.10 | `@testing-library/angular` + `msw` + `@playwright/test` — тестирование | см. `TESTING_STRATEGY.md` | 🟡 Q1 |

> **Почему `@ngrx/signals` а не классический NgRx?**  
> Меньше бойлерплейта (нет `actions`/`reducers`/`selectors` файлов), реактивность на уровне Angular Signals, лучше подходит для соло-разработчика, нет overhead Runtime при маленькой команде. Переход на классический NgRx — только при команде 3+ чел.

---

## 3. База данных

### ✅ Реализовано (Май 2026)

| # | Улучшение | Файл | Приоритет |
|---|-----------|------|-----------|
| 3.1 | `Dapper` 2.1.35 — быстрые read-query через Dapper (CQRS read side) | `Infrastructure.csproj` | 🟡 Q1 |
| 3.2 | `Microsoft.Extensions.Caching.StackExchangeRedis` — Redis cache-aside | `DependencyInjection.cs` | 🔴 MVP |
| 3.3 | Redis health check (`Degraded`, не `Unhealthy`) — Redis не ронит readiness | `DependencyInjection.cs` | 🔴 MVP |
| 3.4 | **ИСПРАВЛЕНИЕ БАГА:** KZ Local DB health status → `Degraded` вместо `Unhealthy` | `DependencyInjection.cs` | 🔴 MVP |
| 3.5 | `Azure.Storage.Blobs` — архивирование `AuditLogs` в Azure Blob | `Infrastructure.csproj` | 🟡 Q2 |

### 🔲 Ожидает реализации

| # | Улучшение | Приоритет |
|---|-----------|-----------|
| 3.A | Global `ISoftDelete` query filter в `AppDbContext.OnModelCreating` | 🔴 MVP |
| 3.B | `RowVersion` optimistic concurrency на Orders и Inventory → HTTP 409 в ExceptionHandler | 🔴 MVP |
| 3.C | `Outbox` pattern — таблица `OutboxMessages` + Hangfire processor | 🔴 MVP (Kaspi Pay webhooks) |
| 3.D | `CacheAside<T>` helper — stampede protection + try-catch passthrough | 🟡 Q1 |
| 3.E | Standby-реплика KZ Local DB (HA) | 🔴 MVP (см. `RISKS_AND_MITIGATION.md` Risk #14) |

---

## 4. CI/CD

### ✅ Реализовано (Май 2026)

| # | Улучшение | Файл | Приоритет |
|---|-----------|------|-----------|
| 4.1 | `dotnet format --verify-no-changes` — блокирует PR с нарушением форматирования | `deploy-azure1.yml`, `test.yml` | 🟡 Q1 |
| 4.2 | `npm audit --audit-level=high` — CVE-сканирование npm-зависимостей | `test.yml`, `deploy-azure2.yml` | 🔴 MVP |
| 4.3 | **CodeQL SAST** (`github/codeql-action`) — C# + TypeScript анализ безопасности | `test.yml` | 🔴 MVP |

### 🔲 Ожидает реализации

| # | Улучшение | Приоритет |
|---|-----------|-----------|
| 4.A | `infra/main.bicep` — IaC для всех Azure-ресурсов | 🔴 до запуска |
| 4.B | `deploy-infra.yml` — `workflow_dispatch` деплой Bicep-шаблона | 🔴 до запуска |
| 4.C | Container image scanning (Trivy для Docker-образов) | 🟡 Q2 |

---

## 5. Мониторинг и Observability

### ✅ Реализовано (Май 2026)

| # | Улучшение | Файл | Приоритет |
|---|-----------|------|-----------|
| 5.1 | `Microsoft.ApplicationInsights.AspNetCore` 2.22.0 — телеметрия | `Program.cs` / `.csproj` | 🔴 MVP |
| 5.2 | `HealthCheckTelemetryFilter` — фильтрует `/health/*` из AI-трафика | `Telemetry/HealthCheckTelemetryFilter.cs` | 🟡 Q1 |
| 5.3 | `Serilog.Sinks.ApplicationInsights` — conditional sink | `Program.cs` | 🔴 MVP |
| 5.4 | OpenTelemetry OTLP-экспортер (параллельно AI, для будущего Grafana) | `Program.cs` | 🟢 Q3 |

### 🔲 Ожидает реализации

| # | Улучшение | Приоритет |
|---|-----------|-----------|
| 5.A | Azure Monitor alerts: 5xx rate > 5%, /health/ready fail, CPU > 90% | 🔴 Day 1 деплоя |
| 5.B | KZ Local DB on-prem monitor: Hangfire job каждые 5 мин → `TelemetryClient.TrackMetric` | 🟡 Q1 |
| 5.C | Application Insights connection string → Azure Key Vault | 🔴 до запуска |
| 5.D | `TelemetryService` — custom events: `OrderPlaced`, `PaymentFailed` | 🟡 Q1 |

---

## 6. Надёжность интеграций

### 🔲 Ожидает реализации (0% текущий статус)

| # | Улучшение | Приоритет |
|---|-----------|-----------|
| 6.1 | Typed HTTP clients: `IOneCClient`, `IKaspiPayClient`, `ISmsService` с `.AddStandardResilienceHandler()` | 🔴 MVP |
| 6.2 | **SMS dual-provider failover**: SMSC.kz → KazInfoTech (блокирует регистрацию пользователей!) | 🔴 MVP |
| 6.3 | `Inventory.IsStaleInventory` flag + circuit breaker recovery trigger | 🔴 MVP |
| 6.4 | `Payments.TransactionId` UNIQUE INDEX + idempotency check в Kaspi webhook handler | 🔴 MVP |
| 6.5 | WireMock.NET mock для AZS API (spec ещё не получен от партнёра) | 🟡 Q2 |

**Матрица resilience:** см. `TECH_STACK.md` §2б

---

## 7. Безопасность

### ✅ Реализовано (Май 2026)

| # | Улучшение | Файл | Приоритет |
|---|-----------|------|-----------|
| 7.1 | JWT Bearer аутентификация + Authorization | `Program.cs` | 🔴 MVP |
| 7.2 | Rate Limiting: OTP 3 req/час (SlidingWindow), API 100 req/мин (FixedWindow) | `Program.cs` | 🔴 MVP |
| 7.3 | CORS: явный allowlist фронтенд-доменов + credentials для SignalR | `Program.cs` | 🔴 MVP |
| 7.4 | HSTS (max-age=1год, только production) | `Program.cs` | 🔴 MVP |
| 7.5 | Security headers: X-Frame-Options DENY, X-Content-Type-Options nosniff, CSP, Permissions-Policy | `Program.cs` | 🔴 MVP |
| 7.6 | `HangfireAdminAuthorizationFilter` — закрытый Hangfire Dashboard (только Admin) | `Middleware/HangfireAdminAuthorizationFilter.cs` | 🔴 MVP |
| 7.7 | Azure Key Vault integration — секреты вне `appsettings.json` | `Program.cs` | 🔴 до запуска |
| 7.8 | `Destructurama.Attributed` + `[NotLogged]`/`[LogMasked]` — ПДн не попадают в логи | `Program.cs` | 🔴 MVP |

### 🔲 Ожидает реализации

| # | Улучшение | Приоритет |
|---|-----------|-----------|
| 7.A | Role-based policies: `Customer`, `Courier`, `Admin`, `Picker` | 🔴 MVP |
| 7.B | `[Authorize]` на всех контроллерах, явный `[AllowAnonymous]` на публичных | 🔴 MVP |
| 7.C | Refresh token rotation + revocation | 🟡 Q1 |

---

## 8. Производительность

### ✅ Реализовано (Май 2026)

| # | Улучшение | Файл | Приоритет |
|---|-----------|------|-----------|
| 8.1 | Response Compression (Brotli + Gzip) — 60–80% меньше трафика | `Program.cs` | 🟡 Q1 |
| 8.2 | Output Cache (`catalog` policy: 5 мин, VaryByQuery) | `Program.cs` | 🟡 Q1 |
| 8.3 | Redis cache-aside + `abortConnect=False,connectRetry=3` fault tolerance | `DependencyInjection.cs` | 🔴 MVP |

### 🔲 Ожидает реализации

| # | Улучшение | Приоритет |
|---|-----------|-----------|
| 8.A | Mapster source generators — zero-reflection маппинг в рантайме | 🟡 Q1 |
| 8.B | Azure CDN для Angular SPA — `Cache-Control: max-age=31536000, immutable` для хэшированных ассетов | 🟡 Q2 |
| 8.C | Redis Standard C1 (не Basic C0) в production Bicep-шаблоне | 🔴 до запуска |
| 8.D | `CacheAside<T>` stampede protection (`IDistributedLock`) для hot catalog key | 🟡 Q1 |

---

## 9. Developer Experience

### ✅ Реализовано (Май 2026)

| # | Улучшение | Файл | Приоритет |
|---|-----------|------|-----------|
| 9.1 | `Directory.Build.props` — `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest` | `Directory.Build.props` | 🟡 Q1 |
| 9.2 | `global.json` — pin SDK `10.0.100` `latestPatch` | `global.json` | 🟡 Q1 |
| 9.3 | `docker-compose.yml` — локальный SQL Server ×2 + Redis для integration тестов | `docker-compose.yml` | 🟡 Q1 |
| 9.4 | Scalar UI (`/scalar/v1`) вместо Swagger UI — современный OpenAPI-интерфейс | `Program.cs` | 🟡 Q1 |

### 🔲 Ожидает реализации

| # | Улучшение | Приоритет |
|---|-----------|-----------|
| 9.A | Создать `tests/` папку: `DarkStore.UnitTests`, `DarkStore.IntegrationTests`, `DarkStore.ArchitectureTests` | 🔴 MVP |
| 9.B | `Directory.Packages.props` — Central Package Management (одно место для версий) | 🟡 Q1 |
| 9.C | `bruno/` коллекция — pre-configured API-запросы + HMAC-сигнатура Kaspi | 🟡 Q1 |
| 9.D | `infra/main.bicep` — IaC для воссоздания среды за < 30 мин | 🔴 до запуска |

---

## Критический путь (MVP-блокирующие задачи)

```
[Май 2026] CURRENT: Инфраструктура и DI готовы
     ↓
[1] ✅ Создать DarkStore.Application (MediatR handlers, validators, pipeline behaviors, ICurrentUserService)
     ↓
[2] Typed HTTP clients (SMS + Kaspi Pay + 1С) с resilience
     ↓
[3] Создать тест-проекты (создать tests/)
     ↓
[4] IaC Bicep + Azure Key Vault wiring
     ↓
[5] LAUNCH ✅
```

---

## Легенда приоритетов

| Значок | Срок |
|--------|------|
| 🔴 MVP | Блокирует Public Launch — реализовать до первого клиента |
| 🟡 Q1/Q2 | Важно — реализовать в первые 3–6 месяцев |
| 🟢 Q3+ | Желательно — рассмотреть по мере роста |

---

*Создан: Май 2026. Обновлять при каждом завершённом пункте.*

