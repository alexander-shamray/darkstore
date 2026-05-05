# 🔍 Dark Store — Оставшиеся задачи

**Последнее обновление:** 4 мая 2026  
**Статус проекта:** Разработка MVP — Месяц 1

---

## 🔴 ЕДИНСТВЕННАЯ ОТКРЫТАЯ ЗАДАЧА

### Консультация с юристом — Закон РК «О персональных данных» №94-V

**Архитектурное решение уже принято** (гибридная БД):
- 🇰🇿 **KZ Local DB** (Beeline KZ VPS / KAZTELECOM) — `PersonalDataDbContext`: `Users`, `Addresses`, `LoyaltyTransactions`, `Notifications`
- ☁️ **Azure SQL** (Sweden Central) — `AppDbContext`: бизнес-данные без ПДн

**Что осталось сделать:**
- [ ] Нанять казахстанского юриста и верифицировать гибридное решение **до запуска MVP**
- [ ] При необходимости скорректировать архитектуру по рекомендации юриста
- [ ] Обновить `TECH_STACK.md` и `DATABASE_SCHEMA.md` по итогам консультации

---

## 🔴 ОТКРЫТЫЕ ЗАДАЧИ (критические — блокируют запуск)

> Подробный чеклист → **`MANUAL_ACTIONS.md`**

| # | Задача | Где задокументировано |
|---|--------|-----------------------|
| 1 | **Юрист** — ОФД/ККМ + КЗ ПДн + ИП/ТОО + трудоустройство курьеров + бэкапы + СанПиН | `LEGAL_REQUIREMENTS.md` |
| 2 | **SMS-провайдер** — без него регистрация пользователей невозможна (SMSC.kz / KazInfoTech) | `INTEGRATIONS.md` §2а; `CONFIGURATION_GUIDE.md` §10а |
| 3 | **AZS API Gate** — получить spec от партнёра ДО написания кода; поднять WireMock.NET mock при отсутствии sandbox | `INTEGRATIONS.md` §3; `ROADMAP_12_MONTHS.md` Q1 задача 11 |
| 4 | **KZ Local DB HA** — standby-реплика (2 VPS), иначе auth SPOF | `RISKS_AND_MITIGATION.md` Risk #14 |
| 5 | **Courier PWA + Picker PWA** — операции невозможны без интерфейсов | `ROADMAP_12_MONTHS.md` Q2 |
| 6 | **ОФД фискальные чеки** — НК РК обязывает, Kaspi Pay имеет нативный модуль | `LEGAL_REQUIREMENTS.md` §2; `RISKS_AND_MITIGATION.md` Risk #13 |
| 7 | **Application Insights** — включить с Day 1 деплоя, 3 алерта (5xx, health, DTU) | `CONFIGURATION_GUIDE.md` §11 |
| 8 | **IaC Bicep** — создать `infra/main.bicep` до публичного запуска | `TECH_STACK.md` §6 |
| 9 | **Нанять Angular-разработчика** — Месяц 2–3, **не 6** (3 PWA: Courier, Picker, Admin) | `RISKS_AND_MITIGATION.md` Risk #5 |

### Критический путь к запуску

```
[1] Юрист (ОФД + КЗ ПДн + ООО/ИП + курьеры)       → LEGAL_REQUIREMENTS.md
    ↓
[2] SMS OTP провайдер → регистрация пользователей   → INTEGRATIONS.md §2а
    ↓
[3] AZS API Gate (получить spec / поднять mock)     → INTEGRATIONS.md §3
    ↓
[4] KZ Local DB HA (standby-реплика)                → RISKS_AND_MITIGATION.md Risk #14
    ↓
[5] Courier PWA + Picker PWA                        → ROADMAP_12_MONTHS.md Q2
    ↓
[6] ОФД / ККМ зарегистрированы                     → LEGAL_REQUIREMENTS.md §2
    ↓
[7] Application Insights + alerts активны           → CONFIGURATION_GUIDE.md §11
    ↓
[8] ПУБЛИЧНЫЙ ЗАПУСК
```

---

*Обновлено: 4 мая 2026*
