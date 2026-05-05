# 📋 Стратегия логирования и аудита (Logging & Audit Strategy)

**Проект:** Dark Store — быстрая доставка продуктов  
**Локация:** Костанай, Казахстан  
**Дата:** Май 2026

---

## Оглавление

1. [Принципы и подходы](#1-принципы-и-подходы)
2. [Архитектура логирования](#2-архитектура-логирования)
3. [Уровни логов и правила применения](#3-уровни-логов-и-правила-применения)
4. [Serilog — конфигурация и синки](#4-serilog--конфигурация-и-синки)
5. [Обогащение контекста (Log Enrichment)](#5-обогащение-контекста-log-enrichment)
6. [Что логировать по слоям](#6-что-логировать-по-слоям)
7. [MediatR Pipeline Behaviors](#7-mediatr-pipeline-behaviors)
8. [Обработка ошибок и исключений](#8-обработка-ошибок-и-исключений)
9. [Защита ПДн в логах (Закон №94-V)](#9-защита-пдн-в-логах-закон-94-v)
10. [Аудит-трейл (Audit Trail)](#10-аудит-трейл-audit-trail)
11. [Схема таблицы AuditLogs](#11-схема-таблицы-auditlogs)
12. [Абстракция — IAuditService](#12-абстракция--iauditservice)
13. [Что аудировать — примеры событий](#13-что-аудировать--примеры-событий)
14. [Azure Application Insights](#14-azure-application-insights)
15. [Алерты и мониторинг](#15-алерты-и-мониторинг)
16. [Политика хранения логов (Retention)](#16-политика-хранения-логов-retention)
17. [Анти-паттерны — что избегать](#17-анти-паттерны--что-избегать)
18. [Чеклист перед уходом в Production](#18-чеклист-перед-уходом-в-production)

---

## 1. Принципы и подходы

### Три принципа

| Принцип | Что означает |
|---------|-------------|
| **Структурированный** | Каждая лог-запись — это JSON-объект с предсказуемыми полями, а не строка текста. Это позволяет делать сложные запросы типа `WHERE StatusCode = 500 AND UserId = '...'`. |
| **Корреляционный** | Каждый запрос, каждое его действие можно отследить сквозь весь pipeline по `CorrelationId`. |
| **Безопасный** | Нигде в обычных логах по №94-V: ни ФИО, ни телефон, ни адрес доставки не попадают в Azure Log Analytics, Application Insights или сторонние сервисы. |

### Разграничение: Логи vs Аудит

```
+---------------------------------------------------------------------------+
|           Логи (Logs)               |          Аудит (Audit Trail)         |
+-------------------------------------+-------------------------------------+
| Операционный журнал системы         | Бизнес-действия с понятным смыслом  |
| HTTP запросы, ошибки, SQL, jobs     | Кто что сделал, когда и с чем       |
| Хранится в Azure Log Analytics      | Хранится в Azure SQL (AuditLogs)    |
| Retention: 30–90 дней               | Retention: 3–7 лет (compliance)     |
| Аудитория: DevOps, разработчики     | Аудитория: бизнес, юристы, финансы  |
| Могут содержать IP, StatusCode      | Нет ПДн: только UserId (GUID)       |
+---------------------------------------------------------------------------+
```

---

## 2. Архитектура логирования

```
+--------------------------------------------------------------------------+
|                          ASP.NET Core API                                |
|                                                                           |
|  +------------------+    +-------------------+    +------------------+  |
|  | Correlation ID   |    | Serilog Request   |    | Global Exception |  |
|  | Middleware       |--->| Logging Middleware|--->| Handler          |  |
|  +------------------+    +-------------------+    +------------------+  |
|                                   |                                      |
|  +--------------------------------+----------------------------------+  |
|  |              MediatR Pipeline Behaviors                            |  |
|  |  LoggingBehavior --> ValidationBehavior --> AuditBehavior         |  |
|  +-------------------------------------------------------------------+  |
|                                   |                                      |
|  +------------------+    +--------+---------+    +------------------+  |
|  | IAuditService    |    | Application /    |    | Domain Events    |  |
|  | (AuditLogs DB)   |<---| Domain Layer     |--->| Handler Logging  |  |
|  +------------------+    +-----------------+    +------------------+   |
+--------------------------------------------------------------------------+
                                    |
              +---------------------+---------------------+
              |                     |                     |
    +-----------------+  +------------------+  +-------------------+
    | Console (Dev)    |  | Azure App        |  | Azure Log         |
    | File (local)     |  | Insights (Prod)  |  | Analytics (Prod)  |
    +-----------------+  +------------------+  +-------------------+
```

---

## 3. Уровни логов и правила применения

| Уровень | Когда использовать | Примеры | Alerting |
|---------|-------------------|---------|---------|
| `Verbose` | Отладочный уровень — только для локальной / отладки | SQL queries (отключены в prod) | ✗ |
| `Debug` | Подробности в Development | Значения параметров, все события | ✗ |
| `Information` | Нормальная работа бизнес-логики | Новый заказ, запрос доставки, вызов API | ✗ |
| `Warning` | Ситуация, но сервис продолжает работать | Stale inventory, retry события, KZ DB компенсация | → Slack |
| `Error` | Обрыв сервиса, требует немедленного отклика | Payment gateway 500, DB timeout, unhandled exception | → PagerDuty |
| `Fatal` | Критический сбой, требует немедленного вмешательства | Startup failure, host terminated | → PagerDuty + SMS |

### Пример уровней по умолчанию

```json
// appsettings.json (Production)
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning",
        "Hangfire": "Warning"
      }
    }
  }
}

// appsettings.Development.json (Development — все включено)
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

> ⚠️ **Внимание:** Включать `Microsoft.EntityFrameworkCore.Database.Command` на уровень `Information` нельзя в Production — это выведет каждый SQL запрос в открытый и дорогостоящий Azure Log Analytics поток.

---

## 4. Serilog — конфигурация и синки

### Пакеты

```bash
dotnet add package Serilog.AspNetCore                  # core
dotnet add package Serilog.Enrichers.Environment       # EnvironmentName, MachineName
dotnet add package Serilog.Enrichers.Thread            # ThreadId
dotnet add package Serilog.Enrichers.Process           # ProcessId
dotnet add package Serilog.Sinks.ApplicationInsights   # Azure Application Insights
dotnet add package Serilog.Sinks.Async                 # Non-blocking async wrapper
```

### Полная конфигурация (`Program.cs`)

```csharp
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        // Обогащение контекста
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithProcessId()
        .Enrich.WithProperty("Application", "DarkStore.API")
        .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")

        // Development: консоль + файл
        .WriteTo.Async(a => a.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}"))
        .WriteTo.Async(a => a.File(
            path: "logs/darkstore-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 100 * 1024 * 1024,  // 100 MB per file
            rollOnFileSizeLimit: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}"))

        // Production: Azure Application Insights (включать только если InstrumentationKey задан)
        .WriteTo.Conditional(
            condition: _ => !string.IsNullOrEmpty(ctx.Configuration["ApplicationInsights:InstrumentationKey"]),
            configureSink: a => a.Async(s => s.ApplicationInsights(
                services.GetRequiredService<TelemetryConfiguration>(),
                TelemetryConverter.Traces)));
});
```

### Structured logging — правильный подход

```csharp
// ПРАВИЛЬНО — используем именованные параметры (structured properties)
_logger.LogInformation(
    "Order {OrderId} placed by user {UserId} for {TotalAmount:C} — items: {ItemCount}",
    order.Id, order.UserId, order.TotalAmount, order.Items.Count);

// НЕПРАВИЛЬНО — строковая интерполяция (нет индексации, нет фильтрации)
_logger.LogInformation($"Order {order.Id} placed by user {order.UserId}");

// ПРАВИЛЬНО — destructuring объекта (@ prefix)
_logger.LogDebug("Processing command {@Command}", command);

// Не злоупотреблять @ destructuring в Production — может записать ПДн!
// Убедитесь, что запись не содержит PersonalData через политику маскирования.
```

---

## 5. Обогащение контекста (Log Enrichment)

### Correlation ID Middleware

Correlation ID должен быть **первым middleware** в pipeline. Он передаёт `X-Correlation-Id` в каждый запрос и ответ, и добавляет его в `LogContext`.

```csharp
// Middleware/CorrelationIdMiddleware.cs
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? context.TraceIdentifier
                            ?? Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        {
            await next(context);
        }
    }
}

// Extension method
public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    => app.UseMiddleware<CorrelationIdMiddleware>();
```

### Обогащение User Context

Чтобы автоматически добавить `UserId` и `UserRole` в LogContext для всех MediatR handlers:

```csharp
// Application/Common/Behaviors/LoggingBehavior.cs (см. раздел 7)
// В Command/Query handlers:
using (LogContext.PushProperty("UserId", currentUser.Id))
using (LogContext.PushProperty("UserRole", currentUser.Role))
{
    return await next();
}
```

### HTTP Request Logging (Serilog)

```csharp
// Program.cs
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.000}ms [{CorrelationId}]";

    opts.GetLevel = (httpContext, elapsed, ex) =>
        ex is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;

    opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
    {
        diagCtx.Set("CorrelationId",  httpCtx.Items["CorrelationId"]?.ToString() ?? httpCtx.TraceIdentifier);
        diagCtx.Set("UserAgent",      httpCtx.Request.Headers.UserAgent.ToString());
        diagCtx.Set("RemoteIP",       httpCtx.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        diagCtx.Set("RequestMethod",  httpCtx.Request.Method);
        diagCtx.Set("QueryString",    httpCtx.Request.QueryString.HasValue ? httpCtx.Request.QueryString.Value : "");

        if (httpCtx.User.Identity?.IsAuthenticated == true)
        {
            // Только UserId (GUID) — не ФИО, не телефон
            var userId = httpCtx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is not null)
                diagCtx.Set("UserId", userId);
        }
    };
});
```

---

## 6. Что логировать по слоям

### API Layer (Controllers)

| Событие | Уровень | Что включать |
|---------|---------|-------------|
| Входящий запрос | `Information` (Serilog middleware автоматически) | Method, Path, StatusCode, Elapsed |
| Ошибки валидации | `Warning` | CommandType, ValidationErrors (без ПДн) |
| Unauthorized | `Warning` | Path, RemoteIP, CorrelationId |

> Controllers должны быть тонкими — логирование через MediatR behaviors, не в контроллерах напрямую.

### Application Layer (MediatR Handlers)

| Событие | Уровень | Что включать |
|---------|---------|-------------|
| Command начат | `Debug` | CommandType, UserId, CorrelationId |
| Command завершён | `Information` | CommandType, UserId, Duration, ResultId |
| Query завершён | `Debug` | QueryType, Duration, ResultCount |
| Domain Event обработан | `Information` | EventType, AggregateId, CorrelationId |
| Retry попытка | `Warning` | CommandType, AttemptNumber, Reason |

### Infrastructure Layer

| Событие | Уровень | Что включать |
|---------|---------|-------------|
| DB запрос медленный (> 1s) | `Warning` | SQL hash (не сам SQL!), Duration, CorrelationId |
| Redis cache miss | `Debug` | CacheKey, Operation |
| Redis недоступен | `Warning` | Exception.Message, CorrelationId |
| External API успех | `Information` | ServiceName, Endpoint, StatusCode, Duration |
| External API ошибка | `Error` | ServiceName, Endpoint, StatusCode, Exception |
| Circuit Breaker открыт | `Warning` | ServiceName, FailureRatio |
| Hangfire job started | `Information` | JobId, JobType |
| Hangfire job failed | `Error` | JobId, JobType, Exception |

### Domain Layer

Сам Domain Layer **не должен** внедрять логгеры — он генерирует `Domain Events`, которые обрабатываются в Application Layer.

```csharp
// В Domain — только бизнес-логика, никаких ILogger
public class Order : BaseEntity
{
    public void Ship(Guid courierId)
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot ship order in status {Status}");

        Status = OrderStatus.Shipped;
        CourierId = courierId;
        ShippedAt = DateTimeOffset.UtcNow;

        // Domain Event — добавляется к логированию
        AddDomainEvent(new OrderShippedEvent(Id, courierId, ShippedAt.Value));
    }
}

// В Application — обработчик с логированием
public class OrderShippedEventHandler(ILogger<OrderShippedEventHandler> logger)
    : INotificationHandler<OrderShippedEvent>
{
    public Task Handle(OrderShippedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Order {OrderId} shipped — courier {CourierId} assigned at {ShippedAt}",
            notification.OrderId,
            notification.CourierId,
            notification.ShippedAt);

        return Task.CompletedTask;
    }
}
```

---

## 7. MediatR Pipeline Behaviors

Три стандартных behavior в Application Layer — регистрируются в порядке:  
`LoggingBehavior` → `ValidationBehavior` → `AuditBehavior`

### 7.1 LoggingBehavior

```csharp
// Application/Common/Behaviors/LoggingBehavior.cs
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUser.UserId?.ToString() ?? "anonymous";

        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("RequestType", requestName))
        {
            logger.LogDebug(
                "Handling {RequestType} — UserId: {UserId}",
                requestName, userId);

            var sw = Stopwatch.StartNew();
            try
            {
                var response = await next();
                sw.Stop();

                var level = sw.ElapsedMilliseconds > 500
                    ? LogEventLevel.Warning   // медленный handler
                    : LogEventLevel.Debug;

                logger.Log(level,
                    "{RequestType} handled in {ElapsedMs}ms — UserId: {UserId}",
                    requestName, sw.ElapsedMilliseconds, userId);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogError(ex,
                    "{RequestType} failed after {ElapsedMs}ms — UserId: {UserId}",
                    requestName, sw.ElapsedMilliseconds, userId);
                throw;
            }
        }
    }
}
```

### 7.2 ValidationBehavior

```csharp
// Application/Common/Behaviors/ValidationBehavior.cs
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Any())
        {
            // Логируем только названия полей — никаких значений (могут содержать ПДн)
            logger.LogWarning(
                "Validation failed for {RequestType} — Fields: {FailedFields}",
                typeof(TRequest).Name,
                failures.Select(f => f.PropertyName));

            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

### 7.3 AuditBehavior

```csharp
// Application/Common/Behaviors/AuditBehavior.cs
// Работает только на запросах, которые реализуют IAuditableCommand
public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditService auditService,
    ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not IAuditableCommand auditableCommand)
            return await next();

        var response = await next(); // выполнить команду

        // Аудит после успешного выполнения операции
        await auditService.RecordAsync(new AuditEntry
        {
            UserId      = currentUser.UserId ?? Guid.Empty,
            Action      = auditableCommand.AuditAction,
            EntityName  = auditableCommand.AuditEntityName,
            EntityId    = auditableCommand.AuditEntityId?.ToString(),
            // NewValues: безопасные данные (без ПДн — только бизнес-поля)
            NewValues   = auditableCommand.AuditPayload,
            OccurredAt  = DateTimeOffset.UtcNow
        }, ct);

        return response;
    }
}

// Маркер-интерфейс для аудируемых команд
public interface IAuditableCommand
{
    string AuditAction      { get; }
    string AuditEntityName  { get; }
    Guid?  AuditEntityId    { get; }
    object? AuditPayload    { get; }
}
```

### Регистрация behaviors

```csharp
// Application/DependencyInjection.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

    // Порядок регистрации = порядок выполнения
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
});
```

---

## 8. Обработка ошибок и исключений

### Таблица исключений

| Exception Type | HTTP Status | Log Level | Алерт |
|---------------|-------------|-----------|-------|
| `ValidationException` | 400 | `Warning` | ✗ |
| `ArgumentException` | 400 | `Warning` | ✗ |
| `NotFoundException` (custom) | 404 | `Information` | ✗ |
| `UnauthorizedAccessException` | 401 | `Warning` | ✗ (security audit) |
| `ForbiddenException` (custom) | 403 | `Warning` | ✗ (security audit) |
| `InvalidOperationException` | 409 | `Warning` | ✗ |
| `DbUpdateConcurrencyException` | 409 | `Warning` | ✗ |
| `TimeoutException` | 503 | `Error` | ✓ |
| `BrokenCircuitException` | 503 | `Error` | ✓ |
| Все остальные | 500 | `Error` | ✓ |

### Custom Domain Exceptions

```csharp
// Domain/Common/Exceptions/NotFoundException.cs
public sealed class NotFoundException(string entityName, object entityId)
    : Exception($"{entityName} with id '{entityId}' was not found.")
{
    public string EntityName { get; } = entityName;
    public object EntityId   { get; } = entityId;
}

// Domain/Common/Exceptions/ForbiddenException.cs
public sealed class ForbiddenException(string message = "Access denied.")
    : Exception(message);

// GlobalExceptionHandler.cs — маппинг:
var (statusCode, title) = exception switch
{
    ValidationException           => (400, "Validation Failed"),
    NotFoundException             => (404, "Not Found"),
    UnauthorizedAccessException   => (401, "Unauthorized"),
    ForbiddenException            => (403, "Forbidden"),
    InvalidOperationException     => (409, "Business Rule Violation"),
    DbUpdateConcurrencyException  => (409, "Concurrent Modification"),
    TimeoutException              => (503, "Service Unavailable"),
    BrokenCircuitException        => (503, "Dependency Unavailable"),
    _                             => (500, "Internal Server Error")
};
```

### Примеры корректных исключений

```csharp
// ПРАВИЛЬНО — передаём exception как первый параметр (Serilog выведет stack trace)
logger.LogError(ex, "Failed to process payment for order {OrderId}", orderId);

// НЕПРАВИЛЬНО — Message без stack trace
logger.LogError("Failed to process payment: " + ex.Message);

// ПРАВИЛЬНО — Warning для ожидаемых ситуаций
logger.LogWarning(ex, "KZ Local DB unavailable, falling back to degraded mode");

// НЕПРАВИЛЬНО — Error для ожидаемых ситуаций (например, retry)
logger.LogError(ex, "Retry attempt {Attempt} failed", attempt); // Warning правильнее
```

---

## 9. Защита ПДн в логах (Закон №94-V)

### Что НЕЛЬЗЯ логировать

> ⚠️ **Запрещено** — это нельзя в никаком виде попадать в логи, Application Insights, Log Analytics или Sentry:

| Категория | Конкретные поля |
|-----------|-----------------|
| Персональные данные | ФИО, телефон, ИИН, паспорт |
| Контакты | Адрес доставки, email-адрес |
| Платёжные | Номер карты, CVV, данные банковского счёта |
| Аутентификация | Пароль, OTP-коды, refresh tokens, JWT payload |

### Что МОЖНО логировать

| Поле | Описание |
|------|---------|
| `UserId` (GUID) | Идентификатор пользователя — не ПДн |
| `OrderId` (GUID) | Идентификатор заказа |
| `TotalAmount` | Сумма заказа (агрегированная информация) |
| `ItemCount` | Количество позиций |
| `StatusCode` | HTTP код ответа |
| `CourierId` (GUID) | Идентификатор курьера |
| IP-адрес | Для security logging (не хранить в ПДн) |

### Destructuring Attribute — автоматическое маскирование

```csharp
// Специальный атрибут [LogMasked] / встроенная кастомизация через Serilog
// NuGet: Destructurama.Attributed

using Destructurama.Attributed;

public record PlaceOrderCommand : IRequest<Guid>, IAuditableCommand
{
    public Guid UserId       { get; init; }
    public Guid StoreId      { get; init; }
    public decimal Total     { get; init; }

    [NotLogged]  // Полностью скрыть из логов
    public string? ContactPhone  { get; init; }

    [LogMasked(ShowFirst = 0, ShowLast = 0, PreserveLength = false)]
    public string? DeliveryAddress { get; init; }  // Показывать замаскированным

    // IAuditableCommand
    public string AuditAction     => "OrderPlaced";
    public string AuditEntityName => "Order";
    public Guid?  AuditEntityId   => null; // Заполняется после создания
    public object? AuditPayload   => new { UserId, StoreId, Total };
}
```

```csharp
// Infrastructure/DependencyInjection.cs — настройка Destructurama
Log.Logger = new LoggerConfiguration()
    .Destructure.UsingAttributes()  // Включить [NotLogged], [LogMasked]
    .Destructure.ByTransforming<User>(u => new { u.Id, Role = u.Role.ToString() }) // Скрыть ПДн
    // ...
    .CreateLogger();
```

### Запрещено логировать тело HTTP запросов целиком

Никогда не логируйте тело HTTP запроса целиком — только необходимые поля:

```csharp
// НЕПРАВИЛЬНО:
logger.LogDebug("Request body: {Body}", await ReadBodyAsync(context));

// ПРАВИЛЬНО — только безопасные метаданные через DestructuringPolicy или whitelist-подход:
opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
{
    // Только безопасные метаданные запроса
    diagCtx.Set("ContentLength", httpCtx.Request.ContentLength);
    diagCtx.Set("ContentType", httpCtx.Request.ContentType);
    // НЕ логировать тело запроса
};
```

---

## 10. Аудит-трейл (Audit Trail)

### Принципы

- Аудит-лог хранится в **Azure SQL** (`AppDbContext.AuditLogs`) — не в KZ Local DB
- Записи содержат только `UserId` (GUID) — никаких ПДн
- Трейл **неизменяемый**: удалять UPDATE/DELETE только через аудит
- Трейл хранится **несколько лет с большими ограничениями** (по ГК)
- Трейл синхронизируется с **EF SaveChanges перехватчиком**

### Схема записи Audit

```
Пользователь совершает действие
        |
        ↓
[Команда через MediatR pipeline]
        |
        ↓
AuditBehavior.Handle() → IAuditService.RecordAsync()
        |
        ↓
AuditLogs таблица в Azure SQL
```

**Не каждое событие:** сохраняем, запросы, операции изменения данных (без просмотра страниц).

---

## 11. Схема таблицы AuditLogs

```sql
-- Azure SQL (AppDbContext)
CREATE TABLE AuditLogs
(
    Id            UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId        UNIQUEIDENTIFIER  NOT NULL,   -- GUID, не ПДн
    Action        NVARCHAR(100)     NOT NULL,   -- 'OrderPlaced', 'PaymentProcessed', etc.
    EntityName    NVARCHAR(100)     NOT NULL,   -- 'Order', 'Payment', 'Courier'
    EntityId      NVARCHAR(100)     NULL,       -- ID сущности (обычно — GUID, int, etc.)
    OldValues     NVARCHAR(MAX)     NULL,       -- JSON до изменения (для аудита)
    NewValues     NVARCHAR(MAX)     NULL,       -- JSON после изменения (для аудита)
    CorrelationId NVARCHAR(100)     NULL,       -- X-Correlation-Id
    IpAddress     NVARCHAR(50)      NULL,       -- IPv4/IPv6
    UserAgent     NVARCHAR(500)     NULL,
    OccurredAt    DATETIMEOFFSET    NOT NULL DEFAULT SYSUTCDATETIME(),

    INDEX IX_AuditLogs_UserId       (UserId),
    INDEX IX_AuditLogs_EntityId     (EntityName, EntityId),
    INDEX IX_AuditLogs_OccurredAt   (OccurredAt DESC),
    INDEX IX_AuditLogs_Action       (Action)
);
```

### EF Core Entity & Configuration

```csharp
// Domain/Audit/AuditLog.cs
public sealed class AuditLog
{
    public Guid           Id            { get; private set; } = Guid.NewGuid();
    public Guid           UserId        { get; private set; }
    public string         Action        { get; private set; } = default!;
    public string         EntityName    { get; private set; } = default!;
    public string?        EntityId      { get; private set; }
    public string?        OldValues     { get; private set; }
    public string?        NewValues     { get; private set; }
    public string?        CorrelationId { get; private set; }
    public string?        IpAddress     { get; private set; }
    public string?        UserAgent     { get; private set; }
    public DateTimeOffset OccurredAt    { get; private set; } = DateTimeOffset.UtcNow;

    private AuditLog() { } // EF Core

    public static AuditLog Create(AuditEntry entry) => new()
    {
        UserId        = entry.UserId,
        Action        = entry.Action,
        EntityName    = entry.EntityName,
        EntityId      = entry.EntityId,
        OldValues     = entry.OldValues is not null
                            ? JsonSerializer.Serialize(entry.OldValues)
                            : null,
        NewValues     = entry.NewValues is not null
                            ? JsonSerializer.Serialize(entry.NewValues)
                            : null,
        CorrelationId = entry.CorrelationId,
        IpAddress     = entry.IpAddress,
        UserAgent     = entry.UserAgent,
        OccurredAt    = entry.OccurredAt
    };
}
```

```csharp
// Infrastructure/Configurations/AuditLogConfiguration.cs
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.CorrelationId).HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)");

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.OccurredAt);
    }
}
```

---

## 12. Абстракция — IAuditService

```csharp
// Application/Common/Interfaces/IAuditService.cs
public interface IAuditService
{
    Task RecordAsync(AuditEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityName, string entityId, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}

// Application/Common/Interfaces/IAuditService.cs — DTO
public sealed class AuditEntry
{
    public required Guid           UserId        { get; init; }
    public required string         Action        { get; init; }
    public required string         EntityName    { get; init; }
    public string?                 EntityId      { get; init; }
    public object?                 OldValues     { get; init; }
    public object?                 NewValues     { get; init; }
    public string?                 CorrelationId { get; init; }
    public string?                 IpAddress     { get; init; }
    public string?                 UserAgent     { get; init; }
    public DateTimeOffset          OccurredAt    { get; init; } = DateTimeOffset.UtcNow;
}
```

```csharp
// Infrastructure/Audit/AuditService.cs
public sealed class AuditService(
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditService> logger)
    : IAuditService
{
    public async Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
    {
        // Обогащение из HTTP контекста (если доступен)
        var httpCtx = httpContextAccessor.HttpContext;
        var enrichedEntry = entry with
        {
            CorrelationId = entry.CorrelationId
                            ?? httpCtx?.Items["CorrelationId"]?.ToString()
                            ?? httpCtx?.TraceIdentifier,
            IpAddress     = entry.IpAddress
                            ?? httpCtx?.Connection.RemoteIpAddress?.ToString(),
            UserAgent     = entry.UserAgent
                            ?? httpCtx?.Request.Headers.UserAgent.ToString()
        };

        try
        {
            var auditLog = AuditLog.Create(enrichedEntry);
            db.AuditLogs.Add(auditLog);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Аудит не должен ломать основной бизнес-поток
            logger.LogError(ex,
                "Failed to write audit log for action {Action} by user {UserId}",
                entry.Action, entry.UserId);
        }
    }

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityName, string entityId, CancellationToken ct = default)
        => await db.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.OccurredAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
        => await db.AuditLogs
            .Where(a => a.UserId == userId && a.OccurredAt >= from && a.OccurredAt <= to)
            .OrderByDescending(a => a.OccurredAt)
            .AsNoTracking()
            .ToListAsync(ct);
}
```

### EF Core SaveChanges Interceptor (автоматическое логирование Update/Delete)

```csharp
// Infrastructure/Audit/AuditSaveChangesInterceptor.cs
// Автоматически записывает изменения сущностей, реализующих IAuditable
public sealed class AuditSaveChangesInterceptor(
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        RecordAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        RecordAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void RecordAuditEntries(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries<IAuditable>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var oldValues = entry.State == EntityState.Deleted
                ? entry.OriginalValues.Properties
                    .ToDictionary(p => p.Name, p => entry.OriginalValues[p])
                : null;

            var newValues = entry.State == EntityState.Modified
                ? entry.CurrentValues.Properties
                    .Where(p => entry.Property(p.Name).IsModified)
                    .ToDictionary(p => p.Name, p => entry.CurrentValues[p])
                : null;

            context.Set<AuditLog>().Add(AuditLog.Create(new AuditEntry
            {
                UserId     = currentUser.UserId ?? Guid.Empty,
                Action     = entry.State == EntityState.Modified ? "Updated" : "Deleted",
                EntityName = entry.Entity.GetType().Name,
                EntityId   = entry.Property("Id").CurrentValue?.ToString(),
                OldValues  = oldValues,
                NewValues  = newValues,
                OccurredAt = DateTimeOffset.UtcNow
            }));
        }
    }
}

// Маркер-интерфейс для сущностей с аудитом
public interface IAuditable { }
```

---

## 13. Что аудировать — примеры событий

### Бизнес-события (основные)

| Бизнес-событие | Action | EntityName | Что включать |
|---------------|--------|-----------|-----------|
| Новый заказ | `OrderPlaced` | `Order` | OrderId, StoreId, Total, ItemCount |
| Отмена заказа | `OrderCancelled` | `Order` | OrderId, Reason, CancelledBy |
| Доставка завершена | `OrderDelivered` | `Order` | OrderId, CourierId, DeliveredAt |
| Платёж успешен | `PaymentSucceeded` | `Payment` | PaymentId, OrderId, Amount, Method |
| Платёж отклонён | `PaymentFailed` | `Payment` | PaymentId, OrderId, Reason |
| Возврат инициирован | `RefundInitiated` | `Payment` | PaymentId, Amount, Reason |
| Курьер назначен | `CourierAssigned` | `Delivery` | DeliveryId, CourierId, OrderId |
| Статус заказа изменён | `OrderStatusChanged` | `Order` | OrderId, OldStatus, NewStatus |
| Промокод применён | `PromoCodeApplied` | `Order` | OrderId, PromoCode, DiscountAmount |
| Промокод создан/изменён | `PromoCodeModified` | `PromoCode` | AdminUserId, Code, Changes |
| Товар деактивирован | `ProductDeactivated` | `Product` | ProductId, AdminUserId |
| Цена изменена | `ProductPriceChanged` | `Product` | ProductId, OldPrice, NewPrice |

### Безопасность (Security Audit)

| Событие | Action | Приоритет важности |
|---------|--------|-----------------|
| Успешный вход | `UserLoggedIn` | Medium |
| Неудачный вход (3+) | `LoginFailedRepeatedly` | High |
| Смена пароля | `PasswordChanged` | High |
| Выход из системы | `UserLoggedOut` | Low |
| Несанкционированный доступ | `UnauthorizedAccess` | Critical |
| Попытка доступа к чужому ресурсу | `ForbiddenAccessAttempt` | Critical |
| Выдача admin токена | `AdminTokenIssued` | High |

### Пример реализации команды с аудитом

```csharp
// Application/Orders/Commands/PlaceOrder/PlaceOrderCommand.cs
public sealed record PlaceOrderCommand : IRequest<Guid>, IAuditableCommand
{
    public required Guid   UserId   { get; init; }
    public required Guid   StoreId  { get; init; }
    public required List<OrderItemDto> Items { get; init; }
    public required Guid   AddressId { get; init; }

    // IAuditableCommand — только безопасные данные для аудита
    public string   AuditAction     => "OrderPlaced";
    public string   AuditEntityName => "Order";
    public Guid?    AuditEntityId   => null; // заполняется в AuditBehavior после выполнения
    public object?  AuditPayload    => new
    {
        UserId,
        StoreId,
        ItemCount = Items.Count,
        // AddressId — GUID, вп. ок. — не адрес — не ПДн.
    };
}
```

---

## 14. Azure Application Insights

### Конфигурация

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
dotnet add package Serilog.Sinks.ApplicationInsights
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(opts =>
{
    opts.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    opts.EnableAdaptiveSampling = true;       // Авто-сэмплинг при высоком трафике
    opts.EnableDependencyTrackingTelemetryModule = true;  // SQL, HTTP, Redis
});

// Фильтрация шума — исключаем health checks из телеметрии
builder.Services.AddApplicationInsightsTelemetryProcessor<HealthCheckTelemetryFilter>();
```

```csharp
// Infrastructure/Telemetry/HealthCheckTelemetryFilter.cs
public sealed class HealthCheckTelemetryFilter(ITelemetryProcessor next)
    : ITelemetryProcessor
{
    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request &&
            (request.Url?.AbsolutePath.StartsWith("/health") == true))
            return; // Исключаем health check запросы

        next.Process(item);
    }
}
```

### KQL запросы для аналитики

```kql
// Топ 10 самых медленных запросов за последние 24 часа
requests
| where timestamp > ago(24h)
| where success == true
| summarize avg(duration), max(duration), count() by name
| order by avg_duration desc
| take 10

// Частота ошибок по типу исключения
exceptions
| where timestamp > ago(1h)
| summarize count() by type
| order by count_ desc

// Медленные MediatR handlers (> 500ms)
traces
| where timestamp > ago(1h)
| where message contains "handled in"
| extend elapsed = extract("in (\\d+)ms", 1, message, typeof(int))
| where elapsed > 500
| project timestamp, message, elapsed, customDimensions

// Аудит по пользователю (через custom events)
customEvents
| where timestamp > ago(7d)
| where name startswith "Audit_"
| extend userId = tostring(customDimensions.UserId)
| where userId == "YOUR-USER-GUID"
| project timestamp, name, customDimensions
| order by timestamp desc
```

### Custom Telemetry для бизнес-событий

```csharp
// Application/Common/Services/TelemetryService.cs
public sealed class TelemetryService(TelemetryClient telemetry)
{
    public void TrackOrderPlaced(Guid orderId, decimal amount, int itemCount)
    {
        telemetry.TrackEvent("OrderPlaced", new Dictionary<string, string>
        {
            ["OrderId"]   = orderId.ToString(),
            ["ItemCount"] = itemCount.ToString()
        },
        new Dictionary<string, double>
        {
            ["OrderAmount"] = (double)amount
        });
    }

    public void TrackPaymentFailed(Guid orderId, string reason)
    {
        telemetry.TrackEvent("PaymentFailed", new Dictionary<string, string>
        {
            ["OrderId"] = orderId.ToString(),
            ["Reason"]  = reason  // Убедитесь, что reason не содержит ПДн
        });
    }
}
```

---

## 15. Алерты и мониторинг

### Таблица алертов (Azure Monitor / Application Insights)

| Условие | Порог | Окно | Действие |
|---------|-------|--------|---------|
| Частота ошибок HTTP 5xx | > 5% | 5 мин | → PagerDuty (Critical) |
| Средняя задержка запросов | > 2 сек | 10 мин | → Slack #alerts |
| Частота ошибок EF Core | > 10/мин | 5 мин | → PagerDuty |
| Circuit Breaker открыт (1+) | > 0 | мгновенно | → Slack #integrations |
| Hangfire failed jobs | > 5 подряд | 10 мин | → Slack #jobs |
| Health `/health/ready` failed | > 0 | 2 мин | → PagerDuty |
| Память > 85% | > 85% | 5 мин | → Slack #infra |
| CPU > 90% | > 90% | 10 мин | → Slack #infra |
| Алерт `UnauthorizedAccess` | > 10 от пользователя / 1 час | 1 ч | → Security team |
| Алерт `LoginFailedRepeatedly` | > 5 от пользователя / 15 мин | 15 мин | → Security team |

### Health Checks

```csharp
// Infrastructure/DependencyInjection.cs
services.AddHealthChecks()
    // Azure SQL — бизнес-данные
    .AddDbContextCheck<AppDbContext>(
        name: "azure-sql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"])
    // KZ Local DB — ПДн
    .AddDbContextCheck<PersonalDataDbContext>(
        name: "kz-local-db",
        failureStatus: HealthStatus.Degraded,   // Degraded, не Unhealthy — снижение режима
        tags: ["ready"])
    // Redis
    .AddRedis(
        redisConnectionString: config["ConnectionStrings:Redis"]!,
        name: "redis",
        failureStatus: HealthStatus.Degraded,   // Degraded — работаем без кэша
        tags: ["ready"])
    // Hangfire storage
    .AddHangfire(
        setup: _ => { },
        name: "hangfire",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"]);
```

---

## 16. Политика хранения логов (Retention)

| Хранилище | Тип данных | Retention | Примечание |
|-----------|-----------|-----------|------------|
| Azure Log Analytics | HTTP логи, ошибки, события | **90 дней** | Встроенный инструмент в портале |
| Application Insights | Телеметрия, зависимости, ошибки | **90 дней** | Встроенный инструмент AI |
| `logs/darkstore-*.log` (файлы на App Service) | Serilog текстовый sink | **30 дней** | Rolling files, `retainedFileCountLimit: 30` |
| `AuditLogs` (Azure SQL) | Бизнес-события, security | **3 года** и потом 7 лет | Compliance + архивирование обязательно |
| `AuditLogs` (архив, Azure Blob) | Данные 3+ года | **7 лет** | GDPR / Закон о хранении документации |

### Архивирование AuditLogs

```csharp
// Hangfire recurring job — каждые несколько месяцев переносит логи старше 3 лет в Blob
[AutomaticRetry(Attempts = 3)]
public async Task ArchiveOldAuditLogsAsync()
{
    var cutoff = DateTimeOffset.UtcNow.AddYears(-3);

    var oldLogs = await _db.AuditLogs
        .Where(a => a.OccurredAt < cutoff)
        .ToListAsync();

    if (!oldLogs.Any()) return;

    // Сериализуем в JSON и сохраняем в Azure Blob Storage
    var json = JsonSerializer.Serialize(oldLogs);
    var blobName = $"audit-archive/{DateTime.UtcNow:yyyy-MM}/audit-{Guid.NewGuid():N}.json.gz";
    await _blobService.UploadGzippedAsync(blobName, json);

    // Удалить из основного хранилища
    _db.AuditLogs.RemoveRange(oldLogs);
    await _db.SaveChangesAsync();

    _logger.LogInformation("Archived {Count} audit logs older than {Cutoff}", oldLogs.Count, cutoff);
}
```

---

## 17. Анти-паттерны — что избегать

### Логирование

| ❌ Анти-паттерн | ✅ Правильно |
|----------------|------------|
| `logger.LogError(ex.Message)` — без передачи исключения | `logger.LogError(ex, "message")` — с объектом |
| `$"User {user.FullName} placed order"` — строки с ПДн | `"User {UserId} placed order {OrderId}"` |
| Логировать тело HTTP запросов целиком | Логировать только метаданные: метод, путь, статус |
| `LogError` для ожидаемых retry (последний вариант) | `LogWarning` для retry, `LogError` только при необратимой ошибке |
| Логировать JWT или refresh token | Логировать только UserId из токена |
| Синхронные (blocking) логирование в hot path | `WriteTo.Async(...)` — асинхронный sink |
| Inject `ILogger` в Domain-слой | Domain не логирует — только Domain Events |

### Аудит

| ❌ Анти-паттерн | ✅ Правильно |
|----------------|------------|
| Аудировать все входящие запросы | Аудировать только команды изменения состояния |
| UPDATE или DELETE записи напрямую | AuditLogs — добавляет только записи |
| Хранить ПДн в `NewValues` | Только бизнес-идентификаторы (GUID) |
| Аудировать GET-запросы (просмотр) | Аудировать только операции (CUD) и security events |
| Запись в БД из обработчика (синхронно) | Fire-and-forget для некритичного аудита |

---

## 18. Чеклист перед уходом в Production

### Логирование

- [ ] `MinimumLevel.Default` в `appsettings.json` = `Information` (не Debug/Verbose)
- [ ] `Microsoft.EntityFrameworkCore.Database.Command` = `Warning` (не логировать SQL в prod)
- [ ] Application Insights ConnectionString в Azure Key Vault (не в appsettings)
- [ ] `Destructurama.Attributed` подключён и `[NotLogged]` проставлен на все ПДн-поля
- [ ] Correlation ID middleware зарегистрирован первым в pipeline
- [ ] `WriteTo.Async(...)` — все sinks асинхронные
- [ ] `retainedFileCountLimit` настроен (не копятся неограниченно файлы)

### Аудит

- [ ] Таблица `AuditLogs` создана в Azure SQL (запустить миграцию)
- [ ] `AuditBehavior` зарегистрирован в MediatR pipeline
- [ ] Все критические команды реализуют `IAuditableCommand`
- [ ] `AuditLog.Create()` не записывает ПДн-поля
- [ ] Архивирование AuditLogs в Azure Blob настроено как Hangfire recurring job
- [ ] Health check `/health/ready` включает проверку Azure SQL (AuditLogs доступен)

### Мониторинг

- [ ] Azure Monitor алерты настроены (5xx rate, latency, circuit breaker)
- [ ] Security алерты настроены (Unauthorized, LoginFailed)
- [ ] Hangfire Dashboard закрыт авторизацией (не публичный)
- [ ] Log Analytics workspace retention = 90 дней
- [ ] KQL запросы готовы в Application Insights

---

*Документ актуален: Май 2026. Следует пересмотреть при первом старте или при compliance-требованиях.*

