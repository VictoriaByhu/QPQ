# QPQ (Quid pro quo)

Платформа обміну навичками: користувачі пропонують свої навички та домовляються про обмін з іншими.

## Предметна область і контексти

Система поділена на три незалежні контексти, кожен зі своєю базою даних:

| Контекст | Сховище | Основні сутності | Відповідальний |
|---|---|---|---|
| Swaps (Проєкт №1) | SQL Server, ADO.NET + Dapper | Swap, SwapDetails, SwapSkill, SwapStatusHistory | [ваше ім'я] |
| [Catalog] (Проєкт №2) | SQL Server, EF Core | [сутності] | [ім'я] |
| [Reviews] (Проєкт №3) | MongoDB | [колекції] | [ім'я] |

Між базами немає FOREIGN KEY: зв'язки між контекстами забезпечуються ідентифікаторами та синхронізацією копій.

## Політика дублювання даних

| Дані | Джерело (власник) | Де дублюються | Що дублюється | Як синхронізуються |
|---|---|---|---|---|
| Користувачі | [контекст-власник] | Swaps (`Users`) | Id, DisplayName, Email | [подія / API / періодично] |
| Навички | Catalog | Swaps (`Skills`) | Id, Name, Category | [подія / API / періодично] |
| [...] | [...] | [...] | [...] | [...] |

## Розгортання баз даних

Вимоги: SQL Server LocalDB (входить до Visual Studio), `sqlcmd`.

### Проєкт №1 (Swaps)

Скрипти запускають у такому порядку, кожен можна виконувати повторно:

1. `db/p1/schema.sql`: створює базу `QPQ_Swaps`, таблиці, обмеження та індекси;
2. `db/p1/procedures.sql`: створює 5 збережуваних процедур;
3. `db/p1/seed.sql`: додає тестові дані (повторний запуск не створює дублікатів).

```
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i db\p1\schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i db\p1\procedures.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i db\p1\seed.sql
```

### Проєкт №2
[порядок запуску: `db/p2/schema.sql`, `db/p2/seed.sql`]

### Проєкт №3 (MongoDB)
[порядок запуску: `db/p3/collections.js`, `db/p3/seed.js`]

## Структура Swaps

ERD: [docs/erd-swaps.md](docs/erd-swaps.md).

| Таблиця | Призначення | Зв'язок |
|---|---|---|
| Users | локальна копія користувачів | 1:N зі Swaps (ініціатор і виконавець) |
| Skills | локальна копія навичок з Catalog | M:N зі Swaps через SwapSkills |
| Swaps | заявка на обмін | головна сутність |
| SwapDetails | деталі заявки | 1:1 зі Swaps |
| SwapSkills | навички заявки з роллю Offered/Requested | M:N з власним полем |
| SwapStatusHistory | журнал змін статусу | 1:N до Swaps |

Усі основні таблиці мають аудитні колонки (`CreatedAt`, `UpdatedAt`), м'яке видалення (`IsDeleted`, `DeletedAt`) та `RowVersion` для оптимістичної конкурентності.

### Збережувані процедури

| Процедура | Призначення |
|---|---|
| `usp_CreateSwap` | створення заявки в одній транзакції (Swaps, SwapDetails, SwapSkills, історія) |
| `usp_ChangeSwapStatus` | транзакційна зміна статусу з перевіркою прав, дозволених переходів і RowVersion |
| `usp_GetSwapById` | заявка з деталями та навичками |
| `usp_GetUserSwaps` | заявки користувача з фільтрами та пагінацією |
| `usp_SoftDeleteSwap` | м'яке видалення з перевіркою RowVersion |

Статуси: Pending, Accepted, Completed, Rejected, Cancelled.