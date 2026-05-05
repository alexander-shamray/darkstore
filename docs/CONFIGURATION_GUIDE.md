# ⚙️ Руководство по конфигурации (Configuration Guide)

**Проект:** Dark Store — быстрая доставка продуктов  
**Дата:** Май 2026

---

## Обзор

Этот документ описывает **все переменные окружения, секреты и настройки**, необходимые для запуска проекта в разных окружениях (локально, Staging, Production).

> ⚠️ **Никогда не коммить реальные значения секретов в репозиторий.**  
> Используй `appsettings.Development.json` (в `.gitignore`) локально и GitHub Secrets / Azure App Service Configuration для боевых окружений.

---

## Структура конфигурации (ASP.NET Core)

```
appsettings.json                  ← базовые настройки (без секретов)
appsettings.Development.json      ← локальные секреты (в .gitignore!)
appsettings.Staging.json          ← staging-переопределения
appsettings.Production.json       ← prod-переопределения (без секретов)
```

Секреты на боевых серверах хранятся в **Azure App Service → Configuration → Application Settings**.

---

## 1. База данных

### 🇰🇿 KZ Local DB — Персональные данные (Закон РК №94-V)

| Переменная | Тип | Описание | Пример значения |
|-----------|-----|---------|----------------|
| `ConnectionStrings__KzLocalConnection` | Secret | Строка подключения к локальной KZ БД (персональные данные) | `Server=kz-db.beeline.kz;Database=DarkStorePersonalDb;User Id=admin;Password=...;Encrypt=True` |

> 🏠 Хостинг: Beeline KZ VPS или KAZTELECOM. Данные физически в Казахстане.  
> Содержит: `Users`, `Addresses`, `LoyaltyTransactions`, `Notifications`

### ☁️ Azure SQL — Бизнес-данные

| Переменная | Тип | Описание | Пример значения |
|-----------|-----|---------|----------------|
| `ConnectionStrings__AzureConnection` | Secret | Строка подключения к Azure SQL (бизнес-данные) | `Server=tcp:darkstore.database.windows.net;Database=DarkStoreDb;User Id=admin;Password=...;Encrypt=True` |
| `ConnectionStrings__Redis` | Secret | Строка подключения к Redis | `darkstore-redis.redis.cache.windows.net:6380,password=...,ssl=True,abortConnect=False` |

> ☁️ Регион: Sweden Central или UAE North (ближайшие к КЗ).  
> Содержит: `Products`, `Orders`, `Inventory`, `Deliveries`, `Payments` и т.д. — без ПДн.

> ⚠️ **Запрещено** хранить ФИО, телефон, email, адрес в Azure SQL. Только `UserId` (GUID).

---

## 2. JWT / Аутентификация

| Переменная | Тип | Описание | Пример значения |
|-----------|-----|---------|----------------|
| `JwtSettings__Secret` | Secret | Секретный ключ для подписи JWT токенов (минимум 32 символа) | `your-super-secret-key-min-32-chars!!` |
| `JwtSettings__Issuer` | Config | Издатель токена | `https://api.darkstore.kz` |
| `JwtSettings__Audience` | Config | Аудитория токена | `https://darkstore.kz` |
| `JwtSettings__ExpiryMinutes` | Config | Время жизни Access token (минуты) | `60` |
| `JwtSettings__RefreshExpiryDays` | Config | Время жизни Refresh token (дни) | `30` |

**Как сгенерировать `Secret`:**
```bash
# PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

---

## 3. Kaspi Pay

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `KaspiPay__BaseUrl` | Config | Базовый URL API Kaspi Pay | `https://api.kaspi.kz/ecommerce/v2` |
| `KaspiPay__MerchantId` | Secret | ID мерчанта | [Личный кабинет Kaspi](https://kaspi.kz/merchant) |
| `KaspiPay__ApiKey` | Secret | API-ключ для аутентификации | Личный кабинет Kaspi → API настройки |
| `KaspiPay__CallbackUrl` | Config | URL для приёма Webhooks от Kaspi | `https://api.darkstore.kz/webhooks/kaspi` |
| `KaspiPay__IsTestMode` | Config | Тестовый режим (`true` в Dev/Staging) | `true` / `false` |

> 📝 **Тестовая среда Kaspi:** Для Dev и Staging используй Kaspi Pay Sandbox. Запросить доступ через официальную документацию Kaspi.

---

## 4. Интеграция с 1С

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `OneCIntegration__BaseUrl` | Secret | URL публичного сервиса 1С (OData endpoint) | Системный администратор 1С |
| `OneCIntegration__Database` | Config | Имя базы 1С | Системный администратор |
| `OneCIntegration__Login` | Secret | Логин для доступа к 1С | Системный администратор |
| `OneCIntegration__Password` | Secret | Пароль для доступа к 1С | Системный администратор |
| `OneCIntegration__SyncIntervalSeconds` | Config | Интервал синхронизации остатков (секунды) | `300` (5 минут) |
| `OneCIntegration__TimeoutSeconds` | Config | Таймаут запросов к 1С | `30` |
| `OneCIntegration__MaxRetryAttempts` | Config | Максимум попыток при ошибке | `3` |
| `OneCIntegration__CircuitBreakerFailureRatio` | Config | Порог ошибок для открытия circuit breaker (0.0–1.0) | `0.5` |
| `OneCIntegration__CircuitBreakerBreakDurationSeconds` | Config | Длительность паузы при открытом circuit breaker | `60` |

**Пример OData URL:** `http://1c-server.internal:8080/ExchangeWithSite/odata/standard.odata/`

---

## 4а. Resilience-конфигурация (глобальная)

Добавь в `appsettings.json` и переопределяй по окружениям:

```json
{
  "ResilienceSettings": {
    "DefaultRetryAttempts": 3,
    "DefaultRetryDelaySeconds": 2,
    "DefaultTimeoutSeconds": 10,
    "DefaultCircuitBreaker": {
      "FailureRatio": 0.5,
      "SamplingDurationSeconds": 30,
      "BreakDurationSeconds": 60
    }
  },
  "HealthChecks": {
    "EvaluationInterval": "00:00:30",
    "MinimumSecondsBetweenFailureNotifications": 300
  }
}
```

---

## 5. Сеть АЗС (Бонусная программа)

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `GasStation__BaseUrl` | Secret | URL API системы лояльности АЗС | Партнёр (инвестор) |
| `GasStation__ApiKey` | Secret | API-ключ для аутентификации | Партнёр (инвестор) |
| `GasStation__ClientId` | Secret | Client ID (если OAuth 2.0) | Партнёр (инвестор) |
| `GasStation__ClientSecret` | Secret | Client Secret (если OAuth 2.0) | Партнёр (инвестор) |
| `GasStation__PointsPerTenge` | Config | Коэффициент начисления баллов (баллов за 1 тенге) | `0.01` |

---

## 6. Azure OpenAI (ИИ-агент)

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `AzureOpenAI__Endpoint` | Secret | Endpoint ресурса Azure OpenAI | Azure Portal → OpenAI → Keys and Endpoint |
| `AzureOpenAI__ApiKey` | Secret | API-ключ | Azure Portal → OpenAI → Keys and Endpoint |
| `AzureOpenAI__DeploymentName` | Config | Имя деплоя модели | Azure Portal → OpenAI → Model deployments |
| `AzureOpenAI__ModelName` | Config | Название модели | `gpt-4o` / `gpt-4o-mini` |
| `AzureOpenAI__MaxTokens` | Config | Максимальное количество токенов в ответе | `1000` |

**Пример Endpoint:** `https://darkstore-openai.openai.azure.com/`

---

## 7. Azure AI Search (RAG / Поиск товаров)

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `AzureAISearch__Endpoint` | Secret | Endpoint ресурса Azure AI Search | Azure Portal → AI Search → Keys |
| `AzureAISearch__ApiKey` | Secret | Admin API-ключ | Azure Portal → AI Search → Keys |
| `AzureAISearch__IndexName` | Config | Имя индекса товаров | `products-index` |

---

## 8. Azure Blob Storage (Изображения)

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `BlobStorage__ConnectionString` | Secret | Строка подключения к Azure Blob | Azure Portal → Storage Account → Access keys |
| `BlobStorage__ContainerName` | Config | Имя контейнера для изображений товаров | `product-images` |
| `BlobStorage__CdnBaseUrl` | Config | Базовый URL CDN для отдачи изображений | `https://darkstore-cdn.azureedge.net` |

---

## 9. Google Maps

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `GoogleMaps__ApiKey` | Secret | API-ключ Google Maps Platform | [Google Cloud Console](https://console.cloud.google.com) → APIs & Services → Credentials |

**Необходимые API в Google Cloud:**
- Directions API
- Geocoding API
- Places API
- Distance Matrix API

---

## 10. Firebase Cloud Messaging (Push-уведомления)

> 💡 **Архитектура Web Push:**  
> - **Бэкенд** использует `FirebaseAdmin` .NET SDK для **отправки** push-уведомлений (посылает сообщения на FCM-сервер).  
> - **Фронтенд** (Angular PWA) использует Firebase Web SDK для **получения** и отображения push.  
> - `FirebaseAdmin` SDK **по-прежнему нужен** на бэкенде даже при стратегии "Angular PWA + Web Push", поскольку Firebase-токены генерируются на фронте, а сервер должен их отправлять.

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `Firebase__ProjectId` | Config | ID проекта Firebase | [Firebase Console](https://console.firebase.google.com) → Project settings |
| `Firebase__ServiceAccountJson` | Secret | JSON Service Account (для серверной отправки) | Firebase Console → Project settings → Service accounts → Generate new private key |

> 🔐 **Рекомендация:** Хранить `Firebase__ServiceAccountJson` в **Azure Key Vault** (не в App Settings как строку). Ссылаться через `@Microsoft.KeyVault(SecretUri=https://darkstore-kv.vault.azure.net/secrets/firebase-sa/)`. Подробнее — Раздел 15.

> 💡 Для локальной разработки положи `firebase-service-account.json` в корень проекта и добавь в `.gitignore`.

---

## 11. Мониторинг (Azure Application Insights)

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `ApplicationInsights__ConnectionString` | Secret | Connection String для телеметрии | Azure Portal → Application Insights → Properties |

> ⚠️ **Включать с первого дня деплоя** — не откладывать на Q3. Уже настроено в `appsettings.json` шаблоне.

**Минимальные алерты с Day 1** (настроить в Azure Monitor):
1. HTTP 5xx rate > 1% за 5 мин → Telegram-уведомление
2. `/health` endpoint failure → Telegram-уведомление
3. Azure SQL DTU > 80% за 10 мин → Telegram-уведомление

> 🛡️ **Fault Tolerance — Мониторинг (расширенный список алертов):**
>
> | Алерт | Порог | Действие | Приоритет |
> |-------|-------|---------|----------|
> | HTTP 5xx rate | > 1% за 5 мин | Telegram + Auto-restart | Критический |
> | `/health` failure | Любое | Telegram немедленно | Критический |
> | KZ Local DB ping | Timeout > 1 сек | Telegram | Критический |
> | Azure SQL DTU | > 80% за 10 мин | Telegram | Высокий |
> | Redis miss rate | > 90% за 5 мин | Telegram (Redis недоступен?) | Высокий |
> | 1С sync failure | > 3 подряд | Telegram | Высокий |
> | Failed payments | > 3 за 5 мин | Telegram + alert финансисту | Высокий |
> | OTP failures | > 10 за 5 мин (1 IP) | Telegram + block IP автоматически | Средний |
> | Azure Cost | > $20/мес | Email разработчику | Средний |
> | Backup not updated | > 25 часов | Email разработчику | Средний |
> | Response time P95 | > 3 сек | Telegram | Средний |
>
> Используй **Connection String**, а не старый Instrumentation Key.

---

## 10а. SMS-провайдер (OTP-верификация)

> ⚠️ **Критичная интеграция** — без неё регистрация пользователей невозможна.

| Переменная | Тип | Описание | Где взять |
|-----------|-----|---------|----------|
| `SmsGateway__Provider` | Config | Название провайдера (`smsc` / `kazinfotech` / `beeline`) | - |
| `SmsGateway__BaseUrl` | Config | Базовый URL API SMS-провайдера | Документация провайдера |
| `SmsGateway__ApiKey` | Secret | API-ключ для аутентификации | Личный кабинет провайдера |
| `SmsGateway__SenderName` | Config | Имя отправителя (alphanumeric) | Кабинет провайдера / согласование |
| `SmsGateway__OtpTtlMinutes` | Config | TTL OTP-кода (минуты) | `5` |
| `SmsGateway__MaxAttemptsPerHour` | Config | Максимум OTP-запросов с одного телефона в час | `3` |

**Рекомендуемые KZ SMS-провайдеры:**
- [SMSC.kz](https://smsc.kz) — популярный в КЗ, REST API, до 160 символов
- [KazInfoTech](https://kazinfotech.kz) — работает с операторами KZ, дешевле для массовых отправок
- Beeline Business SMS API — есть, если KZ DB хостинг у Beeline

**Интерфейс:**
```csharp
public interface ISmsService
{
    Task<bool> SendOtpAsync(string phoneNumber, string code, CancellationToken ct = default);
}
```

---

## 12. Serilog (Логирование)

Настраивается в `appsettings.json`, секретов не требует:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "ApplicationInsights",
        "Args": { "restrictedToMinimumLevel": "Warning" }
      }
    ]
  }
}
```

---

## 13. CI/CD (GitHub Secrets)

Секреты, которые нужно добавить в **GitHub → Settings → Secrets and variables → Actions**:

### OIDC-аутентификация (вместо Publish Profile)

| Название секрета | Описание | Где взять |
|-----------------|---------|----------|
| `AZURE_CLIENT_ID` | Application (client) ID из App Registration | Azure Portal → Entra ID → App registrations |
| `AZURE_TENANT_ID` | Directory (tenant) ID | Azure Portal → Entra ID → App registrations |
| `AZURE_SUBSCRIPTION_ID` | ID подписки Azure | Azure Portal → Subscriptions |

### Имена App Service

| Название секрета | Описание |
|-----------------|---------|
| `AZURE_WEBAPP_NAME` | Имя Azure App Service (Production) |
| `AZURE_WEBAPP_NAME_STAGING` | Имя Azure App Service (Staging) |

### Telegram-уведомления

| Название секрета | Описание |
|-----------------|---------|
| `TELEGRAM_BOT_TOKEN` | Токен Telegram-бота для уведомлений о деплое |
| `TELEGRAM_CHAT_ID` | Chat ID для уведомлений |

> ❌ `AZURE_WEBAPP_PUBLISH_PROFILE` и `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` больше **не нужны** — заменены на OIDC.

### Как настроить GitHub Environments (ручное подтверждение Production)

1. Перейди в репозиторий → **Settings → Environments → New environment**
2. Создай окружение `staging` (без защиты — деплой автоматический)
3. Создай окружение `production`:
   - Включить **"Required reviewers"** → добавить себя
   - Включить **"Prevent self-review"**: выключить (соло-разработчик)
   - Опционально: **"Wait timer"** — 5 минут после деплоя в Staging

После этого каждый деплой в Production потребует ручного нажатия **"Review deployments → Approve"** в интерфейсе GitHub Actions.

---

## 14. Шаблон `appsettings.json`

Скопируй в `appsettings.json` и заполни реальными значениями в `appsettings.Development.json` (не коммитить!):

```json
{
  "ConnectionStrings": {
    "KzLocalConnection": "",
    "AzureConnection": "",
    "Redis": ""
  },
  "JwtSettings": {
    "Secret": "",
    "Issuer": "https://api.darkstore.kz",
    "Audience": "https://darkstore.kz",
    "ExpiryMinutes": 60,
    "RefreshExpiryDays": 30
  },
  "KaspiPay": {
    "BaseUrl": "https://api.kaspi.kz/ecommerce/v2",
    "MerchantId": "",
    "ApiKey": "",
    "CallbackUrl": "https://api.darkstore.kz/webhooks/kaspi",
    "IsTestMode": true
  },
  "OneCIntegration": {
    "BaseUrl": "",
    "Database": "",
    "Login": "",
    "Password": "",
    "SyncIntervalSeconds": 300,
    "TimeoutSeconds": 30
  },
  "GasStation": {
    "BaseUrl": "",
    "ApiKey": "",
    "ClientId": "",
    "ClientSecret": "",
    "PointsPerTenge": 0.01
  },
  "AzureOpenAI": {
    "Endpoint": "",
    "ApiKey": "",
    "DeploymentName": "gpt-4o-mini",
    "ModelName": "gpt-4o-mini",
    "MaxTokens": 1000
  },
  "AzureAISearch": {
    "Endpoint": "",
    "ApiKey": "",
    "IndexName": "products-index"
  },
  "BlobStorage": {
    "ConnectionString": "",
    "ContainerName": "product-images",
    "CdnBaseUrl": ""
  },
  "GoogleMaps": {
    "ApiKey": ""
  },
  "Firebase": {
    "ProjectId": "",
    "ServiceAccountJson": ""
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ]
  }
}
```

---

## 15. Чеклист перед первым запуском

### Локально (Development):
- [ ] Создан `appsettings.Development.json` (добавлен в `.gitignore`)
- [ ] Заполнена строка подключения к локальной БД (SQL Server / PostgreSQL)
- [ ] Выполнены EF Core миграции: `dotnet ef database update`
- [ ] Kaspi Pay переведён в тестовый режим (`IsTestMode: true`)

### Staging:
- [ ] Создан Azure App Service `darkstore-api-staging`
- [ ] Настроены Application Settings в Azure Portal
- [ ] Используется отдельная БД для Staging
- [ ] Kaspi Pay Sandbox подключён

### Production:
- [ ] Все секреты внесены в Azure App Service → Configuration
- [ ] Включён Application Insights
- [ ] Проверен Health Check: `GET /health`
- [ ] Настроены алерты в Azure Monitor
- [ ] Включён Azure SQL Geo-Redundant Backup

### ✅ Fault Tolerance Checklist (обязателен перед публичным запуском):
- [ ] `EnableRetryOnFailure` настроен для обоих DbContext (Azure SQL + KZ Local)
- [ ] `AddStandardResilienceHandler` добавлен для всех HttpClient (1С, Kaspi, SMS, AZS, Maps)
- [ ] Health Checks для всех зависимостей: `/health` возвращает статус каждого сервиса
- [ ] `abortConnect=False` в Redis connection string
- [ ] KZ Local DB имеет standby-реплику (или план перехода на неё)
- [ ] Hangfire Dashboard `/hangfire` защищён авторизацией (не публичный)
- [ ] Outbox Processor Hangfire job зарегистрирован
- [ ] `IsStaleInventory` fallback реализован и протестирован
- [ ] Deployment Slot `staging` создан, swap-процедура протестирована
- [ ] Rollback procedures задокументированы и протестированы на Staging
- [ ] Azure Cost Alerts настроены ($20, $50, $100 пороги)
- [ ] Telegram алерты работают при 5xx ошибках и при health check failures
- [ ] Chaos тесты (KZ DB unavailable, Redis unavailable, 1С unavailable) проходят

---

## 16. `.gitignore` — что обязательно игнорировать

```gitignore
# Конфигурация с секретами
appsettings.Development.json
appsettings.Local.json
firebase-service-account.json
*.PublishSettings

# Переменные окружения
.env
.env.local
.env.*.local
```

---

## 15. Azure Key Vault (Рекомендуемый бэкенд для секретов)

> Текущий подход хранит все секреты в Azure App Service → Configuration. Это приемлемо на старте, но имеет ограничения: нет ротации, нет аудита доступа, multiline JSON (Firebase) хранится как строка.

**Секреты, которые следует перенести в Key Vault:**
- `Firebase__ServiceAccountJson`
- `JwtSettings__Secret`
- `ConnectionStrings__KzLocalConnection`
- `KaspiPay__ApiKey`

**Подключение Key Vault к App Service:**
```csharp
// Program.cs — добавить до builder.Build()
builder.Configuration.AddAzureKeyVault(
    new Uri("https://darkstore-kv.vault.azure.net/"),
    new DefaultAzureCredential()  // использует Managed Identity App Service
);
```

**Ссылка на секрет в App Settings (вместо значения):**
```
@Microsoft.KeyVault(SecretUri=https://darkstore-kv.vault.azure.net/secrets/JwtSecret/version)
```

**Шаги:**
1. Создать Azure Key Vault в Azure Portal
2. Включить Managed Identity на App Service (System-assigned)
3. Дать роль `Key Vault Secrets User` Managed Identity в Key Vault IAM
4. Перенести секреты из App Settings в Key Vault

---

*Документ создан: Май 2026. Обновлять при добавлении новых интеграций.*

