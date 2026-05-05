# Список всех ручных действий проекта — Чеклист

**Проект:** Dark Store  
**Дата:** Май 2026  
**Назначение:** Все действия, которые **не могут быть автоматизированы** — должны быть сделаны вручную командой, либо одним ответственным человеком.

---

## Легенда

| Символ | Смысл |
|--------|-------|
| ЮР | **Юридическое** — критично важно. Сделать до первого заказа. |
| ИТ | **Техника** — нужно до того как приложение начнёт принимать заказы |
| ОП | **Операция** — Q2–Q3 |
| МА | **Маркет** — маркетинговое решение, может подождать |
| ✓ | выполнено |
| ?? | требует уточнения |

---

## Блок 1 — Юридические и регуляторные действия ЮР

### 1.1 Консультация с юридическим советником
**Важно:** Нужно запланировать консультацию с охватом 5+ конкретных областей данного вопроса.  
**Scope для юриста:**
- [ ] ЮР Проверить соответствие архитектуры хранения ПДн (KZ Local + Azure) по Закону №94-V
- [ ] ЮР Урегулировать вопрос хранения резервных копий KZ Local DB в Azure Blob (законодательно)
- [ ] ЮР Выяснить является ли передача данных через Azure API "трансграничной передачей ПДн"
- [ ] ЮР Новый вопрос: ОФД вид — Что нужно для запуска первого заказа
- [ ] ЮР ОФД и ККМ для интернет-торговли: уточнить закрывает ли Kaspi Pay данный вопрос
- [ ] ЮР Оформление труда курьеров: договор подряда / ИП / ГПХ-курьер
- [ ] ЮР Лицензирование: нужна ли лицензия или разрешение дистанционной торговли
- [ ] ЮР Санитарные/СЭЗ: получить ответ про СЭЗ, что нужно сдать
- [ ] Подтвердить все открытые ЮР-вопросы в письме — ссылаться ЮР  
**Ссылка:** `LEGAL_REQUIREMENTS.md`

---

### 1.2 Регистрация юридического лица
- [ ] ЮР Выбрать форму (ИП / ТОО) — перед заключением с подрядчиком
- [ ] ЮР Зарегистрировать форму [egov.kz](https://egov.kz) или ЦОН
- [ ] ЮР Получить БИН/ИИН
- [ ] ЮР Открыть расчётный счёт в банке, совместимом с Kaspi Pay

---

### 1.3 ОФД подключение
- [ ] ЮР Выбрать оператора ОФД (рекомендация: Kaspi Pay включает чек)
- [ ] ЮР Зарегистрировать в КНП
- [ ] ЮР Протестировать фискальную отчётность **до запуска заказов**

---

### 1.4 Трудовые договорённости
- [ ] ЮР Разработать ТД на старт (рекомендуем использовать базовый ТД)
- [ ] ЮР Разработать шаблон для всех договорников, привлечённых к делу
- [ ] ЮР Уточнить с адвокатом по занятости о транспортном и налоговом праве

---

### 1.5 Документы для пользователей
- [ ] ЮР Составить Политику конфиденциальности (Privacy Policy) — разместить на сайте до запуска
- [ ] ЮР Пользовательское соглашение (Terms of Service) — разместить на запуске

---

## Блок 2 — Инфраструктура и DevOps (перед запуском)

### 2.1 GitHub настройки
- [ ] ИТ **Branch protection rules** для `main`:
  - Settings → Branches → Add rule для `main`
  - ✓ Require status checks to pass: `ИТ Build and Test`
  - ✓ Require branches to be up to date before merging
  - ✓ Restrict force pushes
  - ✓ Require at least 1 approval (если не один)
- [ ] ИТ **GitHub Environments**:
  - `staging`: без ограничений (auto-deploy)
  - `production`: Required reviewers → добавить себя; Wait timer: 5 мин
- [ ] ИТ **GitHub Secrets** (Settings → Secrets → Actions):
  ```
  AZURE_CLIENT_ID        — из App Registration
  AZURE_TENANT_ID        — из Entra ID
  AZURE_SUBSCRIPTION_ID  — из Subscriptions
  AZURE_WEBAPP_NAME      — имя production App Service
  AZURE_WEBAPP_NAME_STAGING — имя staging App Service
  TELEGRAM_BOT_TOKEN     — токен бота
  TELEGRAM_CHAT_ID       — chat ID
  ```
- [ ] ИТ Включить **Dependabot** (Settings → Security → Dependabot → Enable для NuGet + npm)

---

### 2.2 Azure ресурсы (первые — первые)
- [ ] ИТ Создать **App Registration** в Azure Entra ID (для OIDC)
  - Пошагово смотри: `PROJECT_CREATION_STEPS.md` → шаг 8
- [ ] ИТ Добавить **Federated Identity Credential** для GitHub Actions
- [ ] ИТ Создать **Azure App Service** (Production + Staging slots или отдельные Apps):
  - Runtime: .NET 10 (или .NET 9 LTS если .NET 10 нестабилен)
  - OS: Linux
  - Pricing: Basic B1 на старте
- [ ] ИТ Создать **Azure SQL Database** (начать Basic/S0 на старте)
- [ ] ИТ Создать **Azure Cache for Redis** (Basic C0 на старте)
- [ ] ИТ Создать **Azure Application Insights** и добавить Connection String в App Settings
- [ ] ИТ Создать **Azure Blob Storage** для архивирования аудита
- [ ] ИТ Создать **Azure Static Web Apps** для Angular frontend
- [ ] ИТ Настроить **Azure Monitor alerts** (3 критических: 5xx, health, DTU) и настроить в Telegram
- [ ] ИТ Создать **Azure Key Vault** и перенести секреты из App Settings
  - Добавить Managed Identity на App Service
  - Дать роль `Key Vault Secrets User` Managed Identity
- [ ] ИТ Написать **Bicep шаблон** `infra/main.bicep` для инфраструктуры

---

### 2.3 KZ Local DB (VPS — Beeline KZ / KAZTELECOM)
- [ ] ИТ **Арендовать VPS** у Beeline KZ или KAZTELECOM (рекомендация: 2 vCPU, 4GB RAM, 50GB SSD, расположен в КЗ)
- [ ] ИТ **Установить и настроить** SQL Server Express или PostgreSQL
- [ ] ИТ **Настроить firewall** — только разрешённые IP (Azure App Service outbound IPs)
- [ ] ИТ **Настроить SSL/TLS** для подключения к БД
- [ ] ИТ **Настроить автоматическое резервное копирование** (SQL Agent job или pg_dump + cron)
- [ ] ИТ **HA решение**: планируется **второй VPS** (в другом KZ-провайдере) с репликой:
  - SQL Server: Always On Availability Groups или Log Shipping
  - PostgreSQL: Streaming Replication на standby
  - Задокументировать процедуру failover в `OPERATIONS_MANUAL.md`
- [ ] ЮР Уточнить у юриста — нужна ли обязательная **резервная копия KZ DB** в Azure Blob. Если нельзя — искать альтернативу на KAZTELECOM storage
- [ ] ИТ **Автоматический Failover:** провести тестирование primary VPS с выключением, убедиться, что standby обслужит в течение <5 минут. Задокументировать RTO.
- [ ] ИТ **Health Check KZ DB:** добавить `/health/kz-db` endpoint. Azure App Service мониторит этот endpoint. Настроить алерт в Telegram при потере связи.

---

### 2.4 Azure Application Insights — настройка в Day 1
> Полный гид в `CONFIGURATION_GUIDE.md`. Здесь только:

- [ ] ИТ Добавить `ApplicationInsights__ConnectionString` в App Service → Configuration → Application Settings
- [ ] ИТ Настроить **следующие минимальные Alert rules** в Azure Monitor:
  1. HTTP 5xx requests > 5 за 5 мин → Telegram
  2. `/health` endpoint failure → Telegram (немедленно)
  3. SQL DTU > 80% за 10 мин → Telegram
  4. Redis missrate > 90% за 5 мин → Telegram
  5. Response time P95 > 3000ms → Telegram
  6. Azure Cost > $20/день → Email
  7. Backup not updated > 25 часов → Email
- [ ] ИТ Создать **постоянную страницу доступности** в Azure CDN — показывать на случай полного outage
- [ ] ИТ Настроить **Auto-Heal** в Azure App Service (CPU >90% / 5xx spike → auto restart)

---

## Блок 3 — Интеграции (перед первым заказчиком)

### 3.1 SMS-провайдер для OTP до запуска
> Без этого невозможно авторизоваться.

- [ ] ИТ Выбрать провайдера: [SMSC.kz](https://smsc.kz) / KazInfoTech / Beeline SMS
- [ ] ИТ Зарегистрироваться в системе и получить API-ключ
- [ ] ИТ Пополнить баланс SMS
- [ ] ИТ Зарегистрировать **alphanumeric sender name** ("DarkStore") — требует заявку провайдера
- [ ] ИТ Добавить в App Settings: `SmsGateway__ApiKey`, `SmsGateway__BaseUrl`, `SmsGateway__SenderName`
- [ ] ИТ Имплементировать `ISmsService` (сделано в конкретном провайдере)
- [ ] ИТ Протестировать OTP на реальном KZ-номере
- [ ] ИТ **Подключить резервный SMS-провайдер** (другой провайдер, работает как fallback) — Fault Tolerance: `ISmsService` реализует автоматический failover при недоступности primary

---

### 3.2 Kaspi Pay
- [ ] ИТ Зарегистрироваться на [kaspi.kz/merchant](https://kaspi.kz/merchant)
- [ ] ИТ Пройти KYC и загрузить документы
- [ ] ИТ Получить `MerchantId`, `ApiKey`
- [ ] ИТ Протестировать оплату в **Sandbox** среде
- [ ] ИТ Добавить `KaspiPay__MerchantId` и `KaspiPay__ApiKey` в App Settings / Key Vault
- [ ] ИТ Имплементировать HMAC-SHA256 валидацию webhook подписи (`X-Kaspi-Signature`)
- [ ] ЮР Уточнить: закрывает ли ОФД-отчётность Kaspi Pay или нужна отдельная система

---

### 3.3 1С Интеграция
- [ ] ИТ Получить от 1С-разработчика: BaseUrl, Database, Login, Password для OData
- [ ] ИТ Провести только моковые данные 1С-базы для разработки (не production!)
- [ ] ИТ Добавить данные 1С в App Settings / Key Vault
- [ ] ИТ Имплементировать circuit breaker (Polly) для HTTP-запросов 1С:
  - Retry: 3 попытки, exponential backoff (2s, 4s, 8s)
  - Circuit Breaker: открывается после 5 ошибок, восстановление через 60с
  - Timeout: 30с на запрос
- [ ] ИТ Имплементировать fallback: при открытом circuit breaker — `Inventory.IsStaleInventory = true`

---

### 3.4 АЗС привязка
- [ ] ИТ **Получить API документацию** от поставщика-партнёра — нет готовой спецификации по данному контракту
- [ ] ИТ Проверить доступность sandbox/test сервисов
- [ ] ИТ Нет REST API в КЗ — документировать решение через WireMock.NET mock-сервер
- [ ] ИТ Добавить milestone gate в roadmap Q2: "АЗС API specs confirmed ✓" до начала разработки

---

### 3.5 Google Maps
- [ ] ИТ Создать проект в [Google Cloud Console](https://console.cloud.google.com)
- [ ] ИТ Включить APIs: Directions, Geocoding, Places, Distance Matrix
- [ ] ИТ Создать API-ключ и ограничить под домены (только `api.darkstore.kz` и `localhost`)
- [ ] ИТ Добавить `GoogleMaps__ApiKey` в App Settings / Key Vault
- [ ] ИТ Настроить **billing alert** за $100/месяц в Google Cloud
- [ ] ИТ Оценить нагрузку: ~2600 заказов × 4 API calls = ~10,400 calls/день. Оптимизировать использование Distance Matrix

---

### 3.6 Firebase (Push-уведомления)
- [ ] ИТ Создать проект в [Firebase Console](https://console.firebase.google.com)
- [ ] ИТ Включить Cloud Messaging
- [ ] ИТ Создать Service Account JSON (Project settings → Service accounts → Generate new private key)
- [ ] ИТ Сохранить в Key Vault (не в App Settings как секрет!)
- [ ] ИТ Добавить `Firebase__ProjectId` в App Settings
- [ ] ИТ Настроить Firebase Analytics events: `first_order`, `order_placed`, `referral_used`, `promo_applied`

---

## Блок 4 — Backend разработка (критически важно MVP)

### 4.1 Аутентификация до запуска
- [ ] ИТ Имплементировать **OTP Auth flow**:
  1. `POST /auth/send-otp` → отправка СМС через `ISmsService`
  2. `POST /auth/verify-otp` → проверка кода, создание `RefreshToken`, выдача JWT + refresh
  3. `POST /auth/refresh` → обновление JWT по refresh token
  4. `POST /auth/logout` → отзыв refresh token (`IsRevoked = true`)
- [ ] ИТ Создать EF Core миграцию для `OtpCodes` и `RefreshTokens` в `PersonalDataDbContext` (KZ Local DB)
- [ ] ИТ Rate limiting на OTP: 5 попыток всего / час, 3 попытки / телефон / час

---

### 4.2 Orders и доставка до запуска
- [ ] ИТ Имплементировать `PlaceOrderCommand`:
  - Получить `Address` из KZ Local DB
  - Создать `Order` в главной базе (без `AddressId`)
  - Вычислить `DeliveryFee` через `DeliveryFeeCalculator`
  - Сохранить `OutboxMessage` в той же транзакции
- [ ] ИТ Имплементировать **SQL Server SEQUENCE** для `OrderNumber` (`DS-2026-000001`)
- [ ] ИТ Имплементировать `Kaspi Pay webhook` обработчик с HMAC-SHA256 валидацией подписи
- [ ] ИТ Имплементировать `DeliveryFeeCalculator` domain service (тарифы из `FINANCIAL_MODEL.md` §10)

---

### 4.3 CourierProfiles — KZ Local DB ДО
- [ ] ИТ Убрать `FullName` и `Phone` из `Couriers` (Azure SQL)
- [ ] ИТ Создать `CourierProfiles` в `PersonalDataDbContext` (KZ Local DB)
- [ ] ИТ Обновить все запросы, которые используют `Couriers.FullName` / `Couriers.Phone`

---

### 4.4 LoyaltyPoints — корректная логика начисления очков ДО
- [ ] ИТ Убрать поле `LoyaltyPoints` из `Users` (через EF Core миграцию)
- [ ] ИТ Добавить Dapper query: `GetLoyaltyBalanceQuery` с `SELECT SUM(Points) FROM LoyaltyTransactions WHERE UserId = @id`
- [ ] ИТ Кэшировать баланс в Redis с TTL 1 мин (`IDistributedCache`)
- [ ] ИТ Имплементировать optimistic concurrency для начисления баллов

---

### 4.5 Outbox Pattern ДО
- [ ] ИТ Создать `OutboxMessages` таблицу в Azure SQL (миграция)
- [ ] ИТ Имплементировать `OutboxPublisher` — сохранять события в БД в транзакции
- [ ] ИТ Создать Hangfire recurring job `OutboxProcessor` (каждые 30с) — берёт необработанные события и публикует их

---

### 4.6 Hangfire Dashboard защита ДО
- [ ] ИТ Имплементировать `HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter`:
  ```csharp
  public bool Authorize(DashboardContext context)
  {
      var httpContext = context.GetHttpContext();
      return httpContext.User.Identity?.IsAuthenticated == true
          && httpContext.User.IsInRole("Admin");
  }
  ```
- [ ] ИТ В Azure App Service: добавить IP restriction на путь `/hangfire` — только разрешённые IP

---

### 4.7 Resilience (Polly) ДО
- [ ] ИТ Добавить `Microsoft.Extensions.Http.Resilience` NuGet пакет
- [ ] ИТ Настроить resilience policies для всех HttpClient-ов:
  - `1CHttpClient`: retry + circuit breaker + timeout
  - `KaspiPayHttpClient`: retry (идемпотентные запросы) + timeout
  - `GasStationHttpClient`: retry + circuit breaker
  - `GoogleMapsHttpClient`: retry + timeout

---

### 4.8 Global Exception Handler и Middleware ДО
- [ ] ИТ Добавить `app.UseExceptionHandler` с RFC 7807 `ProblemDetails`
- [ ] ИТ Добавить Serilog Request Logging: `app.UseSerilogRequestLogging()`
- [ ] ИТ Добавить Correlation ID middleware (пробрасывать `X-Correlation-Id` заголовок до ответа от клиента)
- [ ] ИТ Задокументировать все коды ответов API в `TECH_STACK.md` или создать `API_ERRORS.md`

---

### 4.9 EF Core конфигурации и схема ДО
- [ ] ИТ Создать папки `Infrastructure/Configurations/*.cs` для всех entity
- [ ] ИТ Проверить все таблицы по разделу "Критические вопросы ДО" в `DATABASE_SCHEMA.md`
- [ ] ИТ Добавить `<Nullable>enable</Nullable>` и `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` в `.csproj` файлы
- [ ] ИТ Имплементировать `AuditLogsInterceptor : SaveChangesInterceptor`
- [ ] ИТ Имплементировать `ProductPriceHistoryInterceptor : SaveChangesInterceptor`

---

### 4.10 Redis кэширование — настройка ДО
- [ ] ИТ Определить TTL и ключи для каждого типа кэша (cache-aside):
  - Каталог продуктов: 5 мин, ключ = `catalog:{categoryId}:{page}`
  - Список категорий: 30 мин, ключ = `categories:tree`
  - Баланс лояльности: 1 мин, ключ = `loyalty:{userId}`
  - Тариф DeliveryFee (геозона): 30 мин, ключ = `geo:{lat}:{lng}`
- [ ] ИТ Использовать `IDistributedCache` — простая замена Redis на in-memory в dev окружении

---

## Блок 5 — Frontend разработка

### 5.1 Courier PWA до первого заказчика
- [ ] ИТ Angular route `/courier` с guards
- [ ] ИТ Список активных заказов, назначенных курьеру
- [ ] ИТ Принять / отклонить заказ
- [ ] ИТ Имплементировать pickup с QR-сканированием (Web API: `MediaDevices.getUserMedia`)
- [ ] ИТ Карта доставки до клиента (Google Maps JS API)
- [ ] ИТ Имплементировать подпись клиента (поле / рисовать через Canvas)
- [ ] ИТ GPS трекинг — отправка координат каждые 30с через SignalR

---

### 5.2 Picker PWA до первого заказчика
- [ ] ИТ Angular route `/picker` с guards
- [ ] ИТ Список заданий, оптимизированный по маршруту склада
- [ ] ИТ Вычерк товаров при наборе, перемещение по позициям
- [ ] ИТ Сканирование штрих-кода товара (Web API BarcodeDetector или `zxing`)
- [ ] ИТ Обход недоступных позиций — flow с подтверждением
- [ ] ИТ Имплементировать "передать к упаковщику" с QR-передачей к курьеру (опционально в MVP)

---

### 5.3 Admin Panel PWA ДО
- [ ] ИТ Angular route `/admin` с guards (только роль Admin)
- [ ] ИТ CRUD продуктов (с загрузкой фото в Azure Blob)
- [ ] ИТ Управление заказами (просмотр, смена статуса заказчику)
- [ ] ИТ Инвентарь (остатки товаров, управление флагом IsStaleInventory)
- [ ] ИТ Лог для отображения данных аналитики
- [ ] ИТ Ссылка на Hangfire Dashboard (только для Admin)

---

### 5.4 Клиентское PWA — базовый флоу ДО
- [ ] ИТ Экран авторизации (поле телефона и OTP с анимацией)
- [ ] ИТ Каталог и поиск (Dapper + Azure AI Search)
- [ ] ИТ Страница продукта
- [ ] ИТ Корзина + адрес (с расчётом DeliveryFee через API)
- [ ] ИТ Оформление заказа с Kaspi Pay redirect
- [ ] ИТ Real-time отслеживание заказа (SignalR + Google Maps)
- [ ] ИТ Отправка Web Push (разрешение и получение при входе)
- [ ] ИТ История заказов
- [ ] ИТ Личный кабинет (профиль + история начислений)
- [ ] ИТ Программа лояльности

---

## Блок 6 — Склад и маркетинг

### 6.1 Найм сотрудников
- [ ] ОП **Angular-разработчик (фронтенд), месяц 2–3** — до старта месяц 6. 3 PWA-приложения по 3 экрана нужно реализовать без помощи.
- [ ] ОП Нанять склад / операционного менеджера (по движению кандидатуры)
- [ ] ОП 3–4 курьера (по движению кандидатуры)
- [ ] ОП 2–3 сборщика (старты)
- [ ] ОП Оператор по 1С (склад, приём/выдача) — при получении интеграции

---

### 6.2 Физические торговые
- [ ] ОП Найти и арендовать склад (~300–400 м²) в Костанае
- [ ] ОП Установить температурные зоны (ambient / chilled / frozen)
- [ ] ОП Настроить IT-оборудование (сканеры для сборки и упаковки, Wi-Fi роутер)
- [ ] ОП Получить санитарные книжки
- [ ] ОП Меблировать/оборудовать стеллажами склад (CapEx ~3.5 млн тг)
- [ ] ОП Настроить автоматические записи журнала контроля сроков годности через `InventoryAdjustments`

---

### 6.3 Аналитика и маркетинг
- [ ] МА Настроить Firebase Analytics в Angular PWA (события: `first_order`, `order_placed`, etc.)
- [ ] МА Добавлять UTM-параметры для всех ссылок (для QR, Telegram, Instagram)
- [ ] МА Добавить поле `ReferralSource` для отслеживания упоминаний
- [ ] МА Настроить Telegram-бот для запроса заказов / поддержки (фаза или helpdesk)
- [ ] МА Заменить Google Sheets CRM на Freshdesk Free / Zoho Desk / Chatwoot (self-hosted)
- [ ] МА Провести Mystery Shopping — тестовый заказ конкурентов из `COMPETITOR_ANALYSIS.md`

---

### 6.4 Финансы
- [ ] ОП Отслеживать break-even в первые месяцы стартапа (~4–8% CoGS отклонение)
- [ ] ОП Откалибровать правила расчёта DeliveryFee (см. `FINANCIAL_MODEL.md` §10)
- [ ] ОП Собирать данные unit-экономики по каждой категории заказа через первые 30 дней работы
- [ ] ОП Учесть бюджет "Google Maps API cost" в финансовом прогнозе

---

### 6.5 Бренд и дизайн
- [ ] МА Разработать логотип и фирменный стиль (см. `BRAND_GUIDELINES.md`)
- [ ] МА Разработать маркетинговые материалы для всех СМИ (QR-кодами, листовки)
- [ ] МА Настроить аккаунты в социальных сетях (Q3)

---

## Блок 7 — Безопасность (отдельные ДО)

- [ ] ИТ **Kaspi webhook HMAC**: имплементировать middleware с `X-Kaspi-Signature` валидацией до первого handler регистра
- [ ] ИТ **OTP rate limiting**: max 3 попытки/телефон/час + max 5 попыток всего/час
- [ ] ИТ **Hangfire Dashboard**: имплементировать `IDashboardAuthorizationFilter` + IP restriction в Azure
- [ ] ИТ **PromoCodes.PerUserLimit**: добавить проверку на уровне БД / application-кода
- [ ] ИТ **`Payments.TransactionId` UNIQUE INDEX**: защита от дублирования webhook
- [ ] ИТ Отслеживать аномалии LoyaltyTransactions через App Insights (custom metric alert)

---

## Блок 8 — Документация (предстоит / обновить)

- [ ] ОП `OPERATIONS_MANUAL.md` — добавить раздел по HA KZ Local DB (процедура failover)
- [ ] ОП `OPERATIONS_MANUAL.md` — добавить раздел по случаю отключения электроснабжения (резервный генератор, backup check, ИБП)
- [ ] ОП `COMPETITOR_ANALYSIS.md` — пополнить результатами теста Mystery Shopping
- [ ] МА `BRAND_GUIDELINES.md` — дополнить финальными графическими элементами и логотипами
- [ ] ИТ Создать `API_ERRORS.md` — каталог всех кодов ответов API

---

## Критический путь (приоритетный порядок)

```
Немедленно (до часов):
  1. Консультация с юристом и ответы на все ЮР-вопросы блока 1
  2. Выбрать SMS-провайдера и получить API-ключ и тест-сообщение
  3. Зарегистрировать Kaspi Pay merchant аккаунт и получить sandbox

Неделя 1 (техническое):
  4. Арендовать KZ Local DB VPS (2 узла для HA)
  5. Настроить GitHub Secrets + Environments + Branch Protection
  6. Создать Azure ресурсы (App Service, SQL, Redis, App Insights)
  7. Настроить Application Insights и базовые алерты
  8. Имплементировать OTP auth flow + SMS провайдер
  9. Нанять Angular-разработчика (фронтенд)

Недели 2–3:
  10. CourierProfiles в KZ Local DB
  11. OrderDeliverySnapshot (решить проблему FK)
  12. Hangfire auth + Outbox pattern + Polly resilience
  13. Запустить Angular shell (базовый маршрутизатор)

Неделя 4 (до первого заказчика):
  14. Courier PWA + Picker PWA
  15. Web Push токены (сбор разрешений)
  16. KZ Local DB HA тестирование
  17. АЗС планирование и уточнение

Недели 5–6:
  18. Набрать сотрудников и провести обучение
```

---

*Обновлён: 4 мая 2026. Обновлять при каждом выполнении Items.*

