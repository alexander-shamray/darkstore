# Технологический стек (Tech Stack)

**Проект:** Dark Store — быстрая доставка продуктов  
**Локация:** Костанай, Казахстан  
**Дата:** Май 2026

---

## 1. Общая архитектура

**Тип архитектуры:** Clean Architecture + Модульный монолит  
**Стиль:** Domain-Driven Design (лёгкая версия)  
**Паттерны:** CQRS (через MediatR), Repository + Unit of Work, Domain Events

**Преимущества выбранной архитектуры:**
- Хорошая разделяемость кода
- Легко тестировать
- Удобно масштабировать в будущем
- Подходит для соло-разработки

---

## 1а. Гибридная архитектура хранения данных (Закон РК №94-V)

> ⚖️ **Правовое основание:** Закон РК «О персональных данных и их защите» №94-V обязывает хранить персональные данные граждан Казахстана **на серверах, физически расположенных в РК**. Azure не имеет региона в Казахстане.

**Решение: Гибридный подход** — два изолированных хранилища данных.

```
┌─────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core API (Azure)                      │
│                  (бизнес-логика, оркестрация)                    │
└──────────────┬───────────────────────────┬──────────────────────┘
               │                           │
               ▼                           ▼
┌──────────────────────────┐   ┌───────────────────────────────┐
│  🇰🇿 KZ Local DB           │   │  ☁️  Azure SQL Database        │
│  SQL Server / PostgreSQL  │   │  (Sweden Central / UAE North) │
│  Провайдер: Beeline KZ    │   │                               │
│  или KAZTELECOM           │   │  Бизнес-данные:               │
│                           │   │  - Products, Categories       │
│  Персональные данные:     │   │  - Orders, OrderItems         │
│  - Users (ФИО, телефон)   │   │  - Inventory, Stores          │
│  - Addresses              │   │  - Deliveries, Couriers       │
│  - LoyaltyTransactions    │   │  - Payments                   │
│  - Notifications          │   │  - PromoCodes                 │
│                           │   │  - AuditLogs                  │
└──────────────────────────┘   └───────────────────────────────┘
```

**Принцип разделения данных:**

| Категория | Где хранится | Примеры полей |
|-----------|-------------|---------------|
| 🔴 **ПДн — хранить в КЗ** | KZ Local DB | ФИО, телефон, email, адрес доставки |
| 🟢 **Бизнес-данные — Azure** | Azure SQL | Товары, заказы (только UserId), склад, курьеры |
| 🟡 **Кэш и сессии** | Azure Redis | JWT-токены, краткосрочные данные |

**Связь между базами:**  
API держит два `DbContext`-а с разными connection strings. `UserId` (GUID) — единственный идентификатор, связывающий записи. Никакие ПДн не попадают в Azure SQL.

**Провайдеры KZ Local хостинга:**
- [Beeline KZ](https://beeline.kz/ru/business/it-solutions) — VPS/Dedicated в Алматы/Астане
- [KAZTELECOM](https://telecom.kz/) — дата-центры Kazakhtelecom в РК
- Собственный сервер в Костанае (при наличии надёжного ЦОД)

---

## 2. Backend (Серверная часть)

х| Компонент              | Технология                  | Версия     | Решение | Назначение |
|------------------------|-----------------------------|------------|---------|----------|
| **Язык**               | C#                          | .NET 10    | ✅ Оставить | Основной язык |
| **Фреймворк**          | ASP.NET Core Web API        | 10.0       | ✅ Оставить | Backend API |
| **Архитектура**        | Clean Architecture + MediatR| -          | ✅ Оставить | Структура проекта |
| **ORM (команды)**      | Entity Framework Core       | 10.0       | ✅ Оставить | Writes, миграции, DDD |
| **ORM (запросы)**      | **Dapper**                  | 2.x        | 🆕 Добавить | Reads — сложные SQL-запросы каталога |
| **CQRS**               | MediatR                     | 12.x       | ✅ Оставить | Команды и запросы |
| **Валидация**          | FluentValidation            | 11.x       | ✅ Оставить | Валидация входных данных |
| **Маппинг**            | Mapster + source generators | 7.x        | ✅ Оставить | Маппинг DTO ↔ Entity (zero reflection) |
| **Логирование**        | Serilog (async sinks)       | 4.x        | ✅ + 🔧 Исправить | Структурированное логирование, `WriteTo.Async()` |
| **ПДн-защита в логах** | **Destructurama.Attributed**| 4.x        | 🆕 Добавить | `[NotLogged]`/`[LogMasked]` на командах |
| **OpenAPI**            | **Microsoft.AspNetCore.OpenApi + Scalar** | 10.x  | 🔄 Заменить Swashbuckle | Современный OpenAPI UI |
| **Аутентификация**     | JWT Bearer + OTP + OAuth 2.0 / OIDC | 10.x       | 🆕 Добавить | JWT авторизация, Social Login (Google, Apple, Microsoft, Meta), 2FA, семейные подписки — см. **[AUTH_AND_IDENTITY.md](AUTH_AND_IDENTITY.md)** |
| **Ограничение запросов** | **ASP.NET Core Rate Limiting** | встроен | 🆕 Добавить | OTP 3/час, API 100 req/мин |
| **CORS + HSTS**        | ASP.NET Core built-in       | встроен    | 🆕 Добавить | Безопасность браузера |
| **Сжатие**             | **Response Compression**    | встроен    | 🆕 Добавить | Brotli + Gzip (−60–80% трафика) |
| **Кэш (L1)**           | **Output Cache**            | встроен    | 🆕 Добавить | In-memory кэш перед Redis |
| **Версионирование API** | Asp.Versioning.Http        | 8.x        | ✅ Оставить | URL `/api/v1/` |
| **Фоновые задачи**     | **Hangfire**                | 1.8.x      | 🆕 Добавить | 1С sync, нотификации, таймауты заказов |
| **Real-time**          | **SignalR**                 | встроен    | 🆕 Добавить | Статус заказа в реал-тайм |
| **Observability**      | **Application Insights + OpenTelemetry** | 2.x / 1.9.x | 🆕 Добавить | APM + будущий OTLP Grafana |
| **Секреты**            | **Azure Key Vault**         | SDK 1.x    | 🆕 Добавить | Connection strings, API keys вне appsettings |
| **Безопасность Hangfire** | `HangfireAdminAuthorizationFilter` | - | 🆕 Добавить | Dashboard доступен только Admin-роли |

---

## 2а. Разбор альтернатив — почему именно так

### EF Core → оставить, но добавить Dapper для чтений

**Проблема EF Core только:** сложные SELECT с JOIN по каталогу, истории заказов и аналитике генерируют неоптимальный SQL. На 1000+ заказов/день это заметно.

**Решение — гибридный подход (CQRS естественно даёт это):**
```
Commands (запись)  →  EF Core   — entity tracking, migrations, DDD events
Queries  (чтение)  →  Dapper    — сырой SQL, 5–10× быстрее для сложных SELECT
```

```csharp
// Query Handler — Dapper (быстро, прозрачно)
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IDbConnection _db; // Dapper

    public async Task<List<ProductDto>> Handle(GetProductsQuery q, CancellationToken ct)
    {
        return (await _db.QueryAsync<ProductDto>(
            "SELECT p.Id, p.Name, p.Price, i.Quantity FROM Products p JOIN Inventory i ON p.Id = i.ProductId WHERE p.IsActive = 1 AND p.CategoryId = @CategoryId",
            new { q.CategoryId }
        )).ToList();
    }
}

// Command Handler — EF Core (entity tracking, domain events)
public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private readonly AppDbContext _db; // EF Core
    // ...
}
```

---

### Swashbuckle → заменить на Scalar + Microsoft.AspNetCore.OpenApi

**Причина:** С .NET 9 Microsoft убрал Swashbuckle из шаблонов проекта. `Microsoft.AspNetCore.OpenApi` — встроенный, быстрый, без зависимостей. **Scalar** — современный UI вместо стандартного Swagger UI.

```bash
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Scalar.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddOpenApi();               // встроенный OpenAPI .NET 10

app.MapOpenApi();                            // /openapi/v1.json
app.MapScalarApiReference();                 // /scalar/v1 — красивый UI
```

**Что теряем:** ничего практически. Scalar умеет всё то же, выглядит лучше, обновляется быстрее.

---

### MediatR → оставить, но с ограничениями

MediatR хорош для этого проекта. Главная ловушка — **overuse**:

| Использование | ✅ Правильно | ❌ Неправильно |
|--------------|-------------|--------------|
| Application-слой | Команды (`PlaceOrderCommand`) | Domain Events внутри Entity |
| | Запросы (`GetProductsQuery`) | Простые CRUD без логики |
| | Пайплайн behaviors (логирование, валидация) | Каждый маленький хелпер |

**Альтернатива на будущее:** `Mediator` (source generators, быстрее) — можно рассмотреть при переходе на Q3+.

---

### FluentValidation → оставить

Лучший вариант в .NET-экосистеме. Альтернатив равного качества нет.

---

### Mapster → оставить (версия исправлена: 7.x, не 12.x)

Лучше AutoMapper (который фактически устарел для новых проектов). Включить source generators для нулевого оверхеда в рантайме:

```bash
dotnet add package Mapster
dotnet add package Mapster.DependencyInjection
```

---

### Нужна ли очередь сообщений?

**Ответ: да, но не Azure Service Bus на MVP. Правильный выбор — Hangfire.**

**Зачем очередь в q-commerce:**

| Задача | Без очереди | С Hangfire |
|--------|------------|-----------|
| 1С sync (остатки) | Polling в хуке запроса — блокирует API | Recurring job каждые 5 мин |
| Таймаут заказа (неоплачен 15 мин → отмена) | Нет механизма | `BackgroundJob.Schedule(cancelOrder, TimeSpan.FromMinutes(15))` |
| Kaspi Pay webhook | Долгая обработка внутри HTTP → таймаут у Kaspi | Ack 200 сразу, обработка в фоне |
| Отправка push-уведомлений | Блокирующий вызов Firebase | Fire-and-forget job |
| Ежедневное начисление бонусов | Нет механизма | Cronjob `"0 2 * * *"` |

**Почему Hangfire (не Azure Service Bus) на MVP:**

| | Hangfire | Azure Service Bus |
|-|---------|-------------------|
| Сложность настройки | 5 минут | Часы (топики, подписки, DLQ) |
| Стоимость | Бесплатно (хранится в SQL) | От $10/мес |
| Персистентность | ✅ SQL Server | ✅ |
| Dashboard | ✅ `/hangfire` — визуальный UI | ❌ только Portal |
| Подходит для | < 10 000 jobs/день | Миллионы сообщений |
| Когда переходить | — | **Conditional**: > 500 заказов/сут стабильно 2 нед., или Hangfire DB > 10GB. Финансовая модель Year 1 (~87 заказов/день) не достигает этого порога — не планировать в Q4. |

```bash
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer
```

```csharp
// Program.cs
builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthorizationFilter() }
});

// Примеры использования:
// Recurring (1С sync каждые 5 минут)
RecurringJob.AddOrUpdate<OneCSyncService>("1c-sync", s => s.SyncInventoryAsync(), "*/5 * * * *");

// Delayed (автоотмена заказа через 15 минут)
BackgroundJob.Schedule<OrderService>(s => s.CancelIfUnpaidAsync(orderId), TimeSpan.FromMinutes(15));

// Fire-and-forget (push после смены статуса)
BackgroundJob.Enqueue<NotificationService>(s => s.SendOrderStatusUpdateAsync(orderId, newStatus));
```

---

### SignalR — добавить для real-time статуса

Q-commerce без real-time — это плохой UX. Клиент обновляет страницу вручную или видит статус с задержкой.

**Что нужно в реал-тайм:**
- Клиент: статус заказа (собирают → едет → доставлен) + местоположение курьера
- Курьер: входящий заказ, изменение маршрута
- Админ: дашборд активных заказов

**Альтернативы:**
- **Polling** (каждые 3 сек) — простейший, но 20× больше запросов к API
- **SSE (Server-Sent Events)** — только сервер → клиент, нет двусторонней связи
- **SignalR** — WebSocket с автофоллбэком, встроен в ASP.NET Core, бесплатно ✅

```csharp
// Hub
public class OrderHub : Hub
{
    public async Task JoinOrderGroup(string orderId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
}

// Вызов из OrderService при смене статуса
await _hubContext.Clients.Group($"order-{orderId}")
    .SendAsync("StatusChanged", new { orderId, status, courierLat, courierLng });
```

**На Angular:** `@microsoft/signalr` (npm package), подключается за 10 строк кода.

---

## 2б. Resilience — устойчивость внешних вызовов (обязательно)

Все `HttpClient`-обёртки для внешних сервисов (1С, Kaspi Pay, AZS, Google Maps) **обязаны** иметь retry, circuit breaker и timeout. 1С нестабильна — её зависание без circuit breaker заблокирует Hangfire workers.

**Пакет (встроен в .NET 8+):**
```bash
dotnet add package Microsoft.Extensions.Http.Resilience
```

**Конфигурация в `Program.cs` / `DependencyInjection.cs`:**
```csharp
builder.Services.AddHttpClient<IOneCClient, OneCClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
    });

// Аналогично для Kaspi Pay, AZS, Google Maps
builder.Services.AddHttpClient<IKaspiPayClient, KaspiPayClient>()
    .AddStandardResilienceHandler();
```

**Fallback при открытом circuit breaker (1С):**
- Продолжать использовать последний известный инвентарь (`Inventory.IsStaleInventory = true`)
- Флаг `IsStaleInventory` видим только в Admin Panel, не клиентам
- При закрытии circuit breaker — немедленный full sync (не ждать следующего cron)
- Установить `MaxRetries` для 1С Hangfire recurring job: не более 5 retries с backoff

**Полная матрица resilience по сервисам:**

| Сервис | Retry | Timeout | Circuit Breaker | Fallback |
|--------|-------|---------|----------------|---------|
| 1С (инвентарь) | 3× exp backoff | 10s | 50% / 30s / 1min break | `IsStaleInventory = true`, последний кэш |
| Kaspi Pay (payment init) | 3× exp backoff | 15s | 50% / 30s / 2min break | "Оплата временно недоступна" |
| Kaspi Pay (webhook) | — (webhook ack сразу) | — | — | Outbox: обработать при восстановлении |
| SMS провайдер | 2× / 3s | 5s | 40% / 60s → switch to secondary | Secondary провайдер → email OTP |
| AZS лояльность | 3× exp backoff | 8s | 50% / 30s / 2min break | Без баллов, sync after recovery |
| Google Maps | 2× | 5s | 30% / 60s / 5min break | Кэш Redis → зональный тариф |
| Azure OpenAI | 2× jitter | 20s | 40% / 60s / 5min break | Популярные категории как замена |
| Firebase FCM | 2× / 5s | 10s | graceful skip | Тихо проигнорировать, лог |

---

## 2в. Глобальная обработка ошибок и Request Logging

**Обязательные middleware (в порядке регистрации в `Program.cs`):**

```csharp
// 1. Correlation ID — должен быть первым
app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();
    ctx.Response.Headers["X-Correlation-Id"] = correlationId;
    using (LogContext.PushProperty("CorrelationId", correlationId))
        await next();
});

// 2. Serilog request logging
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.000}ms [{CorrelationId}]";
});

// 3. Global exception handler — RFC 7807 ProblemDetails
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = ex switch
    {
        NotFoundException        => (404, "Resource not found"),
        ValidationException      => (400, "Validation failed"),
        UnauthorizedAccessException => (401, "Unauthorized"),
        _                        => (500, "Internal server error")
    };
    ctx.Response.StatusCode = status;
    await ctx.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status, Title = title,
        Extensions = { ["correlationId"] = ctx.Response.Headers["X-Correlation-Id"] }
    });
}));
```

**Правило:** никакие необработанные исключения не должны возвращать стектрейсы клиентам в Production (`ASPNETCORE_ENVIRONMENT != Development`).

---

## 2г. Стратегия кэширования Redis

Используем паттерн **cache-aside** через `IDistributedCache` (позволяет fallback на in-memory в Dev).

| Что кэшируем | TTL | Ключ Redis | Примечание |
|-------------|-----|-----------|-----------|
| Каталог продуктов (список) | 5 мин | `catalog:page:{n}:size:{s}:cat:{id}` | Инвалидировать при изменении товара |
| Дерево категорий | 30 мин | `categories:tree` | Редко меняется |
| Баланс лояльности | 1 мин | `loyalty:balance:{userId}` | Вычисляется из LoyaltyTransactions |
| Результат геокодинга | 24 ч | `geocode:{lat}:{lng}` | `Addresses.Latitude/Longitude` уже хранят координаты |
| Distance Matrix | 30 мин | `distance:{storeLat}:{storeLng}:{customerLat}:{customerLng}` | Для расчёта DeliveryFee |

**Правило:** JWT-валидация — **не кэшировать** в Redis (проверяется подписью локально без сетевых вызовов).

```csharp
// DependencyInjection.cs
services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = config["ConnectionStrings:Redis"]);

// В Dev — fallback на in-memory если Redis недоступен
if (env.IsDevelopment())
    services.AddDistributedMemoryCache();
```

> 🛡️ **Fault Tolerance Redis:**
> - Redis downtime ≠ падение системы. Всегда используй `try-catch` вокруг cache operations и продолжай без кэша (пробрось на БД).
> - Настрой `abortConnect=False` в connection string — приложение запустится даже если Redis недоступен при старте.
> - Добавь `connectRetry=3` — попытки переподключения при transient network issues.
> - В Production используй Redis Standard C1 (не Basic C0) — Basic не имеет SLA и не поддерживает failover.
> - Настрой **Cache Stampede protection** для дорогостоящих операций (каталог): Lazy loading с lock `IDistributedLock` при первом промахе кэша.

---

## 2а. Стратегия версионирования API

**Метод:** URL-сегмент (`/api/v{version}/resource`) — наиболее наглядно для клиентов и прокси.

```
/api/v1/products       ← текущая стабильная версия
/api/v2/products       ← новая версия (при breaking changes)
```

### Правила введения новой версии

| Тип изменения | Нужна новая версия? | Пример |
|--------------|---------------------|--------|
| Добавление нового поля в ответ | ❌ Нет | `{ ..., "rating": 4.5 }` |
| Добавление нового необязательного параметра | ❌ Нет | `?includeOutOfStock=true` |
| Удаление поля из ответа | ✅ Да | убрать поле `description` |
| Изменение типа поля | ✅ Да | `price: int` → `price: decimal` |
| Изменение структуры ответа | ✅ Да | плоский объект → вложенный |
| Изменение поведения эндпоинта | ✅ Да | изменение логики сортировки |

### Жизненный цикл версий (Sunset Policy)

```
v1 (текущая) → v2 выходит → v1 получает статус "Deprecated"
                           → v1 работает ещё 6 месяцев
                           → v1 отключается
```

- **Deprecated-заголовок:** устаревшая версия возвращает `Deprecation: true` и `Sunset: <date>` в headers
- **Минимальный срок поддержки deprecated:** **6 месяцев** (достаточно для Mobile/PWA клиентов)
- На MVP допустимо только `v1` — новые версии вводятся при реальной необходимости

### Настройка в `Program.cs`

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // возвращает заголовки api-supported-versions
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";        // v1, v2, ...
    options.SubstituteApiVersionInUrl = true;  // /api/v{version}/ → /api/v1/
});
```

### Пример контроллера

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() { ... }
}

// При появлении breaking change — новый контроллер:
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsV2Controller : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() { ... }  // новая структура ответа
}
```

### Scalar (OpenAPI UI) с версионированием

```csharp
// Program.cs — вместо AddSwaggerGen:
builder.Services.AddOpenApi("v1", options => { options.AddDocumentTransformer(...); });
builder.Services.AddOpenApi("v2", options => { ... });

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Dark Store API";
    options.Servers = [new ScalarServer("https://api.darkstore.kz")];
});
// Доступно: /scalar/v1, /scalar/v2
```

---

## 3. Frontend (Клиентская часть)

| Компонент           | Технология              | Версия    | Назначение |
|---------------------|-------------------------|-----------|----------|
| **Фреймворк**       | Angular                 | 20+       | Основной frontend |
| **Язык**            | TypeScript              | 5.x       | Типизация |
| **Стилизация**      | Tailwind CSS            | 4.x       | Быстрая стилизация |
| **UI-компоненты**   | PrimeNG / Angular Material | -      | Готовые компоненты |
| **Состояние**       | **`@ngrx/signals`** (NgRx Signal Store) | - | Реактивное состояние без бойлерплейта NgRx Classic |
| **HTTP-клиент**     | Angular HttpClient      | -         | Запросы к API |
| **Формы**           | Reactive Forms          | -         | Работа с формами |
| **PWA**             | @angular/pwa + Service Worker | -  | Установка на главный экран, офлайн-режим, Web Push |
| **Real-time**       | `@microsoft/signalr`    | -         | OrderHub WebSocket |
| **Карты**           | `@angular/google-maps`  | -         | Трекинг курьера (Q2) |
| **Push-уведомления**| `firebase` + `@angular/fire` | -    | FCM Web Push токены (Q3) |

### Почему `@ngrx/signals` а не классический NgRx?

| | `@ngrx/signals` (Signal Store) | NgRx Store (Classic) |
|-|--------------------------------|----------------------|
| Бойлерплейт | 🟢 ~30% меньше кода | 🔴 Actions + Reducers + Selectors + Effects |
| Реактивность | Angular Signals — нативно | RxJS Observables |
| Подходит для | Соло/малая команда | Команда 3+ человек |
| Производительность | Лучше (Zone-less ready) | Хорошая |
| Когда переходить на NgRx Classic | Команда > 3 чел + сложная state topology | — |

> 🛡️ **Fault Tolerance — Frontend:**
>
> **HTTP Resilience (Angular `HttpClient`):**
> ```typescript
> // Глобальный interceptor с retry для идемпотентных запросов
> // src/app/core/interceptors/retry.interceptor.ts
> intercept(req: HttpRequest<any>, next: HttpHandler) {
>   const isIdempotent = ['GET', 'HEAD', 'OPTIONS'].includes(req.method);
>   if (!isIdempotent) return next.handle(req);
>   return next.handle(req).pipe(
>     retry({ count: 2, delay: (error, attempt) =>
>       error.status >= 500 ? timer(attempt * 1000) : throwError(() => error) })
>   );
> }
> ```
>
> **Offline Support (PWA Service Worker):**
> - Кэшировать страницы каталога и корзины в Service Worker (стратегия `StaleWhileRevalidate`).
> - При потере сети — показывать закешированный каталог с баннером "Нет подключения — показываем последние данные".
> - Оформление заказа при отсутствии сети — мягко блокировать с сообщением "Для оформления нужен интернет".
>
> **SignalR Reconnect:**
> ```typescript
> const connection = new HubConnectionBuilder()
>   .withUrl('/hubs/order')
>   .withAutomaticReconnect([0, 2000, 10000, 30000]) // пауза перед каждой попыткой
>   .build();
> connection.onreconnecting(() => showReconnectingBanner());
> connection.onreconnected(() => hideReconnectingBanner());
> connection.onclose(() => showOfflineBanner());
> ```
>
> **Global Error Handler:**
> ```typescript
> @Injectable()
> export class GlobalErrorHandler implements ErrorHandler {
>   handleError(error: any) {
>     // Логировать в Application Insights / Sentry
>     // Показать Toast: "Что-то пошло не так. Попробуйте обновить страницу."
>     // Не отображать stack trace пользователю
>   }
> }
> ```
>
> **NgRx Error States:** Каждый Effect должен обрабатывать ошибки (`catchError`) и диспатчить `*Failure` action вместо propagation error.

---

## 4. База данных

### 🇰🇿 KZ Local — Персональные данные (обязательно в РК по Закону №94-V)

| Компонент         | Технология                     | Назначение |
|-------------------|--------------------------------|----------|
| **ПДн БД**        | SQL Server / PostgreSQL        | Хранение персональных данных граждан РК |
| **Хостинг ПДн**   | Beeline KZ VPS / KAZTELECOM    | Физически в Казахстане |
| **DbContext**     | `PersonalDataDbContext` (EF Core) | Изолированный контекст для ПДн |

### ☁️ Azure — Бизнес-данные

| Компонент       | Технология                   | Назначение |
|-----------------|------------------------------|----------|
| **Основная БД** | Azure SQL Database           | Товары, заказы, склад, доставка |
| **Регион**      | Sweden Central / UAE North   | Ближайшие к КЗ регионы Azure |
| **DbContext**   | `AppDbContext` (EF Core)     | Бизнес-данные без ПДн |
| **Кэш**         | Azure Cache for Redis        | Кэширование сессий (без ПДн) |
| **Поиск**       | Azure AI Search              | Поиск товаров (только каталог) |

---

## 5. Хостинг и Инфраструктура

### 🇰🇿 KZ Local (персональные данные)

| Компонент              | Технология                    | Этап использования     |
|------------------------|-------------------------------|------------------------|
| **ПДн сервер**         | Beeline KZ VPS (2 CPU, 4GB RAM) | С первого дня (MVP)  |
| **ПДн БД**             | SQL Server Express / PostgreSQL | MVP                  |
| **Бэкап ПДн**          | Локальный SQL Agent → Azure Blob | Ежедневно            |

### ☁️ Azure (бизнес-логика и инфраструктура)

| Компонент              | Технология                    | Этап использования     |
|------------------------|-------------------------------|------------------------|
| **Хостинг Backend**    | Azure App Service             | MVP (первые 6–9 мес)   |
| **Хостинг Frontend**   | Azure Static Web Apps         | MVP                    |
| **Промежуточный этап** | Azure Container Apps          | При росте нагрузки     |
| **Полный масштаб**     | Azure Kubernetes Service (AKS)| При необходимости      |
| **Хранилище**          | Azure Blob Storage            | Изображения, файлы     |
| **CDN**                | Azure CDN                     | Статический контент    |

---

## 6. CI/CD и DevOps

| Компонент       | Технология                  | Назначение |
|-----------------|-----------------------------|----------|
| **Репозиторий** | GitHub                      | Хранение кода |
| **CI/CD**       | GitHub Actions              | Автоматическая сборка и деплой |
| **SAST**        | **GitHub CodeQL**           | Статический анализ C# + TypeScript на уязвимости |
| **IaC**         | Azure Bicep                 | Инфраструктура как код — **обязательно до публичного запуска** |
| **Мониторинг**  | Azure Application Insights  | Мониторинг и алерты |
| **Логи**        | Azure Log Analytics         | Сбор и анализ логов |
| **Secrets**     | Azure Key Vault             | Secrets management — connection strings, API keys вне кода |
| **Форматирование** | `dotnet format`          | Enforced CI-gate — `--verify-no-changes` блокирует PR |
| **Vuln Scan**   | `dotnet list package --vulnerable` + `npm audit` | CVE-сканирование .NET и npm |

### IaC — Bicep (обязательный артефакт до запуска)

Создать Bicep-шаблон в `infra/main.bicep` до публичного запуска. Воссоздание среды после катастрофы должно занимать < 30 минут, а не требовать ручных click-ops действий.

**Ресурсы, которые должен покрывать шаблон:**

| Azure-ресурс | Параметры |
|-------------|-----------|
| App Service Plan (B1 → P1V3) | staging + prod slots |
| Azure App Service | `.NET 10`, Linux |
| Azure SQL Database | S1, geo-redundant backup |
| Azure Cache for Redis | Basic C0 (dev) / Standard C1 (prod) |
| Azure Blob Storage | LRS, контейнер `product-images` |
| Azure Application Insights | connected to Log Analytics workspace |
| Azure Static Web Apps | для Angular PWA |

**Pipeline:**
```yaml
# .github/workflows/deploy-infra.yml — workflow_dispatch (ручной запуск)
on:
  workflow_dispatch:
    inputs:
      environment:
        type: choice
        options: [staging, production]
```

> 📌 Пример команды деплоя: `az deployment group create --resource-group darkstore-rg --template-file infra/main.bicep --parameters environment=production`

---

## 7. ИИ и Интеллектуальные сервисы

| Сервис              | Технология              | Назначение |
|---------------------|-------------------------|----------|
| **ИИ-агент**        | Azure OpenAI Service    | Чат-бот, рекомендации товаров |
| **Модели**          | GPT-4o / GPT-4o-mini    | Основные модели |
| **RAG**             | Azure AI Search + OpenAI| Поиск по каталогу товаров |
| **Анализ тональности** | Azure Text Analytics  | Анализ отзывов (позже) |

---

## 8. Интеграции

| Сервис              | Технология / Протокол                    | Статус |
|---------------------|------------------------------------------|--------|
| **1С**              | OData / Web-сервисы                      | MVP    |
| **Kaspi Pay**       | REST API + Webhooks                      | MVP    |
| **Сеть АЗС**        | REST API                                 | Q2     |
| **Google Maps**     | Google Maps Platform API                 | Q2     |
| **Firebase FCM**    | Web Push (Firebase Web SDK) через Angular PWA | Q3 |

> 📱 **Стратегия мобильных уведомлений:** Используем **Angular PWA + Firebase Web Push** (без отдельного нативного приложения).  
> Android Chrome и iOS Safari 16.4+ поддерживают Web Push. Это даёт 95%+ охват целевой аудитории без отдельной кодовой базы.  
> Нативное мобильное приложение (**Flutter**) рассматривается как опция в Year 2 при наличии спроса от пользователей.

---

## 9. Инструменты разработки

| Инструмент          | Назначение                     |
|---------------------|--------------------------------|
| **JetBrains Rider** | Основная IDE (Backend)         |
| **Visual Studio Code** | Frontend + конфигурация      |
| **Azure Portal**    | Управление облаком             |
| **Postman / Bruno** | Тестирование API               |
| **Scalar**          | Документация API (заменяет Swagger UI) |
| **Hangfire Dashboard** | Мониторинг фоновых задач (`/hangfire`) |
| **Git**             | Система контроля версий        |
| **Docker**          | Контейнеризация (Testcontainers в CI, позже prod) |

---

## 9а. Тестовый стек

> Полная документация: `TESTING_STRATEGY.md`

### Backend

| Пакет | Назначение |
|-------|-----------|
| **xUnit** | Test runner |
| **FluentAssertions** | Читаемые assertions |
| **NSubstitute** | Мокирование зависимостей |
| **Bogus** | Генерация тестовых данных |
| **Microsoft.AspNetCore.Mvc.Testing** | WebApplicationFactory — реальные HTTP тесты |
| **Testcontainers.MsSql + Redis** | SQL Server + Redis в Docker для Integration тестов |
| **Respawn** | Быстрый сброс БД между тестами |
| **WireMock.Net** | Mock внешних API (1С, Kaspi, SMS-шлюз) |
| **NetArchTest.Rules** | Clean Architecture enforcement |

### Frontend (Angular 20)

| Пакет | Назначение |
|-------|-----------|
| **Jest + jest-preset-angular** | Unit тест runner (вместо Karma) |
| **@testing-library/angular** | DOM-ориентированное тестирование компонентов |
| **msw (Mock Service Worker)** | Mock HTTP в тестах |
| **@playwright/test** | E2E тесты (Chromium, Android Chrome, iPhone Safari) |

### CI Quality Gates

| Gate | Порог | Действие при провале |
|------|-------|---------------------|
| Backend line coverage | ≥ 70% | Блокирует PR мерж |
| Vulnerable packages | 0 high/critical | Блокирует PR мерж |
| Architecture tests | 100% pass | Блокирует PR мерж |
| E2E Staging | 100% critical pass | Блокирует деплой в Production |

---

## 10. Почему выбран именно этот стек?

| Преимущество                    | Обоснование |
|----------------------------------|-----------|
| **.NET 10 + Rider**              | Высокая продуктивность, отличная поддержка C# (LTS версия), глубокая интеграция с Azure |
| **Clean Architecture + MediatR**| Хорошая структура кода, легко масштабировать и тестировать |
| **Angular**                     | Современный, мощный, отличная типизация, большая экосистема |
| **Azure**                       | Лучшая интеграция с .NET, удобные managed-сервисы, гибкая масштабируемость |
| **Azure OpenAI**                | Простая интеграция ИИ, хорошая производительность и безопасность |
| **GitHub Actions**              | Бесплатно, удобно, отличная интеграция с Azure |

---

## 11. План эволюции стека

| Период       | Что меняется                                                      |
|--------------|-------------------------------------------------------------------|
| **MVP**      | .NET 10 + Angular PWA + Azure App Service + Hangfire + SignalR + JWT + OTP Auth + Rate Limiting + HSTS + Security Headers |
| **Q1**       | Dapper reads + Output Cache + Response Compression + Mapster source gen + `@ngrx/signals` + **Social Login (OAuth 2.0: Google, Apple, Microsoft, Meta)** + **2FA (TOTP)** |
| **Q2**       | `@angular/google-maps` (курьерская карта) + PWA offline + IaC Bicep finalized + **Семейные подписки + Регистрация по приглашению** |
| **Q3**       | Firebase Web Push + `@angular/fire` + Dapper для сложных reads (аналитика) |
| **6–12 мес** | Переход на Azure Container Apps                                   |
| **Q4+**      | Azure Service Bus — **conditional** (только если > 500 заказов/сут стабильно, финмодель Year 1 этого не предполагает) |
| **12+ мес**  | Возможный переход на AKS + микросервисы                           |
| **Year 2+**  | Нативное приложение на Flutter (при спросе от пользователей)      |
| **Будущее**  | OpenTelemetry + Grafana (OTLP endpoint уже реализован — просто включить), Event-Driven (Azure Service Bus) |

---

---

*Документ актуален: Май 2026. Ревизия — раз в квартал.*
