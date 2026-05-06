# 📗 Операционный индекс — Dark Store

> Порядок чтения для операционного менеджера, нового сотрудника или партнёра.  
> Не требует технических знаний.

---

## 🎯 Шаг 1 — Понять суть проекта (5 мин)

| Документ | Что даёт |
|---------|---------|
| [DARK_STORE_PROJECT_SUMMARY.md](./DARK_STORE_PROJECT_SUMMARY.md) | Идея, УТП, целевая аудитория, формат dark store, ключевые преимущества |
| [COMPETITOR_ANALYSIS.md](./COMPETITOR_ANALYSIS.md) | Матрица конкурентов (Glovo, Wolt, Kaspi, местные), конкурентные преимущества DarkStore |

---

## 💰 Шаг 2 — Понять бизнес-модель (15 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 2.1 | [FINANCIAL_MODEL.md](./FINANCIAL_MODEL.md) | Юнит-экономика, точка безубыточности, прогноз выручки, расходы, Food Waste/Spoilage, формула DeliveryFee |
| 2.2 | [SOLUTION_COMPARISON.md](./SOLUTION_COMPARISON.md) | Сравнение собственной разработки vs готовых платформ (Арзан ALEM): реальные затраты, ставки команды, franchise-риски, 3 финансовых сценария |
| 2.3 | [TEAM_STRUCTURE.md](./TEAM_STRUCTURE.md) | Кто нужен, роли, ответственности, штатное расписание по этапам |

---

## 🗺️ Шаг 3 — Понять клиента и рынок (20 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 3.1 | [CUSTOMER_JOURNEY_MAP.md](./CUSTOMER_JOURNEY_MAP.md) | Путь клиента от первого контакта до повторного заказа; боли и точки восторга |
| 3.2 | [MARKETING_STRATEGY.md](./MARKETING_STRATEGY.md) | Позиционирование, каналы привлечения, бюджет, KPI маркетинга |
| 3.3 | [GROWTH_HACKS.md](./GROWTH_HACKS.md) | Быстрые способы набрать первых клиентов, реферальные механики, промо (Firebase Remote Config A/B) |
| 3.4 | [BRAND_GUIDELINES.md](./BRAND_GUIDELINES.md) | Цвета, типографика, тон общения, единый стиль для команды и партнёров |

---

## ⚙️ Шаг 4 — Как работает дарксторе ежедневно (20 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 4.1 | [OPERATIONS_MANUAL.md](./OPERATIONS_MANUAL.md) | Процессы приёмки, сборки, доставки, возвратов; температурные зоны; стандарты качества; расписание; стратегия бэкапа БД |
| 4.2 | [CUSTOMER_SUPPORT_GUIDE.md](./CUSTOMER_SUPPORT_GUIDE.md) | Скрипты поддержки, типовые проблемы и ответы, SLA, эскалация |

---

## ⚠️ Шаг 5 — Чего опасаться (15 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 5.1 | [RISKS_AND_MITIGATION.md](./RISKS_AND_MITIGATION.md) | 15 рисков (1С, спрос, KZ Local DB SPOF, ОФД/ККМ, payment fraud) с приоритетами и стратегиями снижения |
| 5.2 | [LEGAL_REQUIREMENTS.md](./LEGAL_REQUIREMENTS.md) | ОФД/ККМ, выбор юрлица (ИП/ТОО), Закон о ПДн, трудоустройство курьеров (ТК РК vs ИП), СанПиН, чеклист перед запуском |
| 5.3 | [LOGGING_AND_AUDIT_STRATEGY.md](./LOGGING_AND_AUDIT_STRATEGY.md) | Журнал аудита бизнес-событий, политика хранения (AuditLogs 3 года → архив 7 лет), матрица аудируемых событий (заказы, оплаты, безопасность), соответствие Закону РК №94-V |

---

## 📅 Шаг 6 — Что планируется и когда (10 мин)

| # | Документ | Что даёт |
|---|---------|---------|
| 6.1 | [ROADMAP_12_MONTHS.md](./ROADMAP_12_MONTHS.md) | Квартальный план на 12 месяцев: MVP → рост → масштаб; Courier/Picker/Admin PWA в Q2 |
| 6.2 | [MVP_PLAN.md](./MVP_PLAN.md) | **Источник правды по срокам MVP** — Optimistic (6 мес, soft launch конец октября 2026) vs Realistic (8 мес, soft launch конец декабря 2026). Пошаговый план по вехам M0–M5: операционные требования, KPI, gate публичного запуска |

---

## 🔍 Справочно — открытые задачи

| Документ | Что даёт |
|---------|---------|
| [MANUAL_ACTIONS.md](./MANUAL_ACTIONS.md) | Полный чеклист всех ручных действий: юрист, Azure, GitHub, интеграции, операции — с критическим путём к запуску |

---

## Быстрый старт — для нового сотрудника склада / курьера

```
DARK_STORE_PROJECT_SUMMARY  →  OPERATIONS_MANUAL  →  CUSTOMER_SUPPORT_GUIDE
```

## Быстрый старт — для операционного менеджера

```
FINANCIAL_MODEL  →  TEAM_STRUCTURE  →  OPERATIONS_MANUAL  →  RISKS_AND_MITIGATION  →  LEGAL_REQUIREMENTS
```

## Быстрый старт — для маркетолога / партнёра

```
CUSTOMER_JOURNEY_MAP  →  MARKETING_STRATEGY  →  GROWTH_HACKS  →  BRAND_GUIDELINES
```

---

*Обновлён: Май 2026 — добавлены COMPETITOR_ANALYSIS, BRAND_GUIDELINES, LEGAL_REQUIREMENTS, MANUAL_ACTIONS, LOGGING_AND_AUDIT_STRATEGY, **SOLUTION_COMPARISON**; обновлены описания (риски 12→15, Food Waste в финмодели, ОФД/СанПиН в операциях, Firebase Remote Config в маркетинге)*
