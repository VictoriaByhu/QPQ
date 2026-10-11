# UserSkills API (Лаб. 2: ADO.NET + Dapper)

Мікросервіс піддомену **Skills**: які навички людина пропонує (`Offer`) і які хоче вивчити (`Want`),
мови викладання та вікна доступності. База: `QPQ_UserSkills` (SQL Server, схема з Лаб. 1, `db/p1/userskills.*.sql`).
Між сервісами передаються лише ідентифікатори (`UserId`, `SkillId`); персональних даних тут немає.

## Структура

```
Services/UserSkills/
├── UserSkills.Domain   моделі, константи, доменні винятки (спільні для Dal і Bll)
├── UserSkills.Dal      репозиторії (ADO.NET + Dapper), Unit of Work, SqlExceptionTranslator
├── UserSkills.Bll      сервіси, DTO, профіль AutoMapper
├── UserSkills.Api      контролери, DomainExceptionHandler, Swagger, Serilog
└── UserSkills.Tests    unit-тести BLL (NSubstitute) і перевірка мапінгу
```

Залежності спрямовані в один бік: `Api -> Bll -> Dal`, `Domain` підключають і `Dal`, і `Bll`.
Контролер не звертається до репозиторію напряму, SQL існує лише в `Dal` та в процедурах бази.

| Шар | Що реалізовано |
|---|---|
| ADO.NET (без Generic) | `LanguageRepository`: `SqlCommand`, параметри, транзакція UoW |
| Dapper + `BaseDapperRepository<T>` | `SkillRepository`, `UserSkillRepository` (multi-mapping через `QueryMultipleAsync`, виклики `usp_*`, TVP) |
| Dapper | `UserSkillStatusHistoryRepository` |
| Unit of Work | одне `SqlConnection` і одна `SqlTransaction` на HTTP-запит (Scoped) |

## Запуск через .NET Aspire (одна команда)

1. Розгорніть базу на сервері (скрипти ідемпотентні; потрібна роль `dbcreator`):

   ```bash
   sqlcmd -S "<host>,<port>" -U <login> -C -f 65001 -i db/p1/userskills.schema.sql
   sqlcmd -S "<host>,<port>" -U <login> -C -f 65001 -i db/p1/userskills.procedures.sql
   sqlcmd -S "<host>,<port>" -U <login> -C -f 65001 -i db/p1/userskills.seed.sql
   ```

2. Збережіть рядок підключення в User Secrets AppHost (пароль не потрапляє в git):

   ```bash
   cd Platform.AppHost
   dotnet user-secrets set "ConnectionStrings:UserSkillsDb" "Server=<host>,<port>;Database=QPQ_UserSkills;User Id=<login>;Password=<password>;Encrypt=True;TrustServerCertificate=True;"
   ```

   Для `SwapsDb` потрібен свій рядок (його налаштовує власник Swaps).

3. Запуск:

   ```bash
   dotnet run --project Platform.AppHost
   ```

   Відкрийте Aspire Dashboard за посиланням з консолі: ресурс `userskills-api` має статус *Running*.
   Swagger: `<url з Dashboard>/swagger`.

### Запуск без Aspire

```bash
dotnet user-secrets set "ConnectionStrings:UserSkillsDb" "<рядок підключення>" --project Services/UserSkills/UserSkills.Api
dotnet run --project Services/UserSkills/UserSkills.Api
```

Рядок можна перевизначити змінною оточення: `ConnectionStrings__UserSkillsDb=...`.

## Автентифікація

Користувача видає Identity (claim `sub`). Доки Identity не підключено, у Development
допускається заголовок `X-User-Id: <guid>` (у Swagger кнопка **Authorize**).

## Ендпоінти

| Метод і шлях | Опис | Успіх | Помилки |
|---|---|---|---|
| `GET /api/user-skills` | мої навички, фільтри `type`, `status`, пагінація | 200 | 400, 401 |
| `GET /api/user-skills/{id}` | запис з деталями, мовами, доступністю | 200 | 404 |
| `POST /api/user-skills` | додати навичку (**транзакція**) | 201 + `Location` | 400, 404, 409 |
| `PUT /api/user-skills/{id}` | оновити рівень, деталі, мови, доступність | 200 | 400, 403, 404, 409 |
| `POST /api/user-skills/{id}/status` | змінити статус (**транзакція**: запис + журнал) | 200 | 400, 403, 404, 409 |
| `DELETE /api/user-skills/{id}?rowVersion=` | м'яке видалення | 204 | 400, 403, 404, 409 |
| `GET /api/user-skills/{id}/history` | журнал статусів (лише власник) | 200 | 403, 404 |
| `GET /api/skills`, `/api/skills/{id}` | локальна копія довідника Catalog (лише читання) | 200 | 404 |
| `GET /api/languages`, `/api/languages/{code}` | довідник мов | 200 | 404 |

Приклади запитів — у `UserSkills.Api/UserSkills.Api.http` (Rider / Visual Studio) та нижче:

```bash
curl -X POST http://localhost:5000/api/user-skills \
  -H "X-User-Id: 11111111-1111-1111-1111-111111111111" -H "Content-Type: application/json" \
  -d '{"skillId":1,"type":"Offer","level":4,"languages":[{"languageCode":"uk","proficiency":5}],"slots":[{"dayOfWeek":1,"startTime":"18:00","endTime":"20:00"}]}'
```

`RowVersion` повертається у відповідях (base64) і передається назад під час `PUT`, зміни статусу та видалення.
У query-рядку символ `+` треба кодувати як `%2B`.

## Помилки: від процедури до HTTP-статусу

`THROW 500xx` у процедурі -> `SqlExceptionTranslator` (DAL) -> доменний виняток (BLL) -> `DomainExceptionHandler` (API) -> `ProblemDetails`.

| Виняток | HTTP | Коди бази |
|---|---|---|
| `NotFoundException` | 404 | 50002, 50010 |
| `ValidationException` | 400 | 50020, 50023, 50030 |
| `ForbiddenException` | 403 | 50014 |
| `BusinessConflictException` | 409 | 50011 (RowVersion), 50012 (перехід статусу), 50013, 50021, 2601, 2627 |
| інше | 500 | текст помилки клієнту не віддається |

## Транзакції, ізоляція та конкурентність

- Бізнес-операції `POST`, `PUT`, `status`, `DELETE` виконуються в одній транзакції Unit of Work з
  рівнем `ReadCommitted` (за замовчуванням SQL Server). `POST` об'єднує перевірку навички
  (`SkillRepository`), мов (`LanguageRepository`) і створення запису (`UserSkillRepository`, `usp_AddUserSkill`).
- Процедури додатково беруть `UPDLOCK, HOLDLOCK` на рядок, що змінюється, і перевіряють `RowVersion`:
  це **оптимістична** конкурентність (конфлікт виявляється під час запису і дає 409) плюс короткочасне
  **песимістичне** блокування лише одного рядка.
- Компроміс: вищий рівень ізоляції (`Serializable`) дає менше аномалій читання, але більше блокувань
  і ризик взаємних блокувань (deadlocks). Для цього сервісу `ReadCommitted` достатній: записи різних
  користувачів не перетинаються, а один запис захищено блокуванням і `RowVersion`.
- Таймаут команд — стандартний для `SqlClient` (30 с). При помилці виконується `Rollback`,
  часткових змін немає.

## Тести

```bash
dotnet test Services/UserSkills/UserSkills.Tests
```

Покрито: валідність конфігурації AutoMapper (`AssertConfigurationIsValid`), успішне створення з комітом,
відкат і повторне викидання винятку для неіснуючої навички, невідомої мови та конфлікту репозиторію,
перевірка слотів і пагінації до звернення до бази, чужий журнал (403), неіснуючий запис (404).

## Перевірка перед здачею

1. У Swagger (`/swagger`) викличте кожен ендпоінт і перевірте статуси.
2. `GET /api/user-skills/999999` -> **404** у форматі ProblemDetails, не 500.
3. Змініть статус двічі підряд на `Archived` або передайте стару `RowVersion` -> **409** у форматі ProblemDetails.
4. `POST` з неіснуючим `skillId` або `languageCode` -> 404 / 400, і в базі не з'являється часткових рядків.
5. Передайте в текстове поле значення зі спецсимволом (наприклад, `O'Brien` в `description`): помилки SQL немає, усі значення передаються параметрами.
6. `dotnet run --project Platform.AppHost`: у Dashboard `userskills-api` має статус *Running*.
