# QPQ (Quid pro quo)

Платформа обміну навичками: користувачі пропонують свої навички та домовляються про обмін з іншими.

## Предметна область і контексти

Домен розбито на три піддомени, у кожному по три API. Кожен API має власну базу даних.

| Піддомен | API |
|---|---|
| Skills | Catalog, UserSkills, Matching |
| Swaps | Swaps, Sessions, Messages |
| Reviews | Reviews, Reputation, Moderation |

Персональні дані (ім'я, email, аватар) зберігає зовнішній сервіс Identity (Duende IdentityServer / Keycloak). Він видає `UserId` (claim `sub`), а доменні сервіси зберігають лише цей ідентифікатор.

### Піддомен Swaps: три незалежні контексти

| Контекст | Сховище | База | Сутності, якими він володіє |
|---|---|---|---|
| Swaps (Проєкт №1) | SQL Server, ADO.NET + Dapper | `QPQ_Swaps` | Swap, SwapDetails, SwapSkill, SwapStatusHistory |
| Sessions (Проєкт №2) | SQL Server, EF Core | `QPQ_Sessions` | Session, SessionSkill, SessionOutcome, SessionFormat (довідник) |
| Messages (Проєкт №3) | MongoDB | `QPQ_Messages` | conversations, messages, attachments |

Кожна сутність належить рівно одному контексту. Таблиці `Skills` (у Swaps і Sessions), `AcceptedSwaps` (у Sessions) та колекція `conversations` (частково) є лише локальними копіями чужих даних, їх власник вказаний у політиці дублювання. Між базами немає FOREIGN KEY і спільних таблиць: зв'язок забезпечується ідентифікаторами та подіями.

## Політика дублювання даних

| Дані | Власник | Де зберігається копія | Що дублюється | Як підтримується узгодженість |
|---|---|---|---|---|
| Користувачі | Identity | ніде не дублюються | лише `UserId` (GUID) у полях `InitiatorId`, `PartnerId`, `CreatedBy`, `UpdatedBy`, `ChangedByUserId`, `CreatedByUserId`, `senderId`, `uploadedBy`, `participants.userId` | ім'я для відображення береться з токена або з Identity; подія `UserDeleted` очищає дані користувача |
| Навички | Catalog | Swaps (`Skills`) | `Id`, `Name` | подія `SkillChanged` оновлює локальну копію |
| Навички | Catalog | Sessions (`Skills`) | `Id`, `Name` | подія `SkillChanged` оновлює локальну копію |
| Прийнятий обмін | Swaps | Sessions (`AcceptedSwaps`) | `SwapId`, `InitiatorId`, `PartnerId`, `Status` | події `SwapAccepted` (створює копію), `SwapCompleted` і `SwapCancelled` (оновлюють статус), див. Лаб. 08 |
| Прийнятий обмін | Swaps | Messages (`conversations`) | `swapId`, `swapStatus`, `participants` (`userId`, `role`) | події `SwapAccepted` (створює розмову), `SwapCompleted` і `SwapCancelled` (оновлюють `swapStatus`), див. Лаб. 08 |
| Останнє повідомлення | Messages (`messages`) | Messages (`conversations.lastMessage`) | `messageId`, `senderId`, `preview`, `sentAt` | оновлюється разом із додаванням нового повідомлення |
| Назва вкладення | Messages (`attachments`) | Messages (`messages.attachments[].fileName`) | `fileName` | задається під час створення повідомлення, назва вкладення не змінюється |

## Розгортання баз даних

Вимоги: SQL Server LocalDB (входить до Visual Studio), `sqlcmd`, MongoDB та `mongosh`.

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

### Проєкт №2 (Sessions)

1. `db/p2/schema.sql`: створює базу `QPQ_Sessions`, таблиці, обмеження та індекси;
2. `db/p2/seed.sql`: додає довідник форматів і тестові дані (повторний запуск не створює дублікатів).

```
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i db\p2\schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i db\p2\seed.sql
```

### Проєкт №3 (Messages, MongoDB)

1. `db/p3/collections.js`: створює колекції з валідацією `$jsonSchema` та індекси в базі `QPQ_Messages`;
2. `db/p3/seed.js`: додає тестові дані (повторний запуск не створює дублікатів).

```
mongosh db\p3\collections.js
mongosh db\p3\seed.js
```

## Структура контекстів

ERD: [docs/erd-swaps.md](docs/erd-swaps.md). Опис контекстів: [docs/contexts.md](docs/contexts.md). Приклади документів MongoDB: [docs/document-examples.md](docs/document-examples.md).

### Swaps (`QPQ_Swaps`)

| Таблиця | Призначення | Зв'язок |
|---|---|---|
| Skills | локальна копія навичок з Catalog | M:N зі Swaps через SwapSkills |
| Swaps | заявка на обмін (`InitiatorId`, `PartnerId`, `Status`) | головна сутність |
| SwapDetails | деталі заявки | 1:1 зі Swaps |
| SwapSkills | навички заявки з роллю Offered/Requested | M:N з власним полем `Role` |
| SwapStatusHistory | журнал змін статусу | 1:N до Swaps |

Таблиця `Swaps` має аудитні колонки `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, м'яке видалення (`IsDeleted`, `DeletedAt`) та `RowVersion` для оптимістичної конкурентності. Решта таблиць мають `CreatedAt`, а там, де можливе паралельне редагування, `UpdatedAt` і `RowVersion`.

#### Обмеження

| Тип | Приклади |
|---|---|
| PK | `Swaps.Id`, `SwapDetails.SwapId`, складений `SwapSkills (SwapId, SkillId, Role)` |
| FK | `SwapDetails → Swaps`, `SwapSkills → Swaps` і `→ Skills`, `SwapStatusHistory → Swaps` |
| UNIQUE | `UX_Skills_Name`: назва навички не повторюється серед активних |
| CHECK | допустимі статуси, `InitiatorId <> PartnerId`, роль `Offered/Requested`, `DurationMinutes > 0` |
| DEFAULT | `Status = 'Pending'`, `CreatedAt = SYSUTCDATETIME()`, `IsDeleted = 0` |

Каскадного видалення немає: усі FK мають `NO ACTION`. Обміни видаляються м'яко (`IsDeleted`), тому фізичне видалення не використовується, а історія статусів не повинна зникати разом із заявкою.

#### Індекси

| Індекс | Під який запит створено |
|---|---|
| `IX_Swaps_Initiator_Status` | `usp_GetUserSwaps` для ролі Initiator: відбір за `InitiatorId` і `Status`, сортування за `CreatedAt` (колонка в `INCLUDE`), лише активні записи |
| `IX_Swaps_Partner_Status` | те саме для ролі Partner |
| `IX_SwapSkills_Skill` | пошук обмінів за навичкою та перевірка FK під час оновлення навичок |
| `IX_SwapStatusHistory_Swap` | вибірка історії статусів конкретного обміну в хронологічному порядку |
| `UX_Skills_Name` | унікальність назв і пошук навички за назвою |

#### Збережувані процедури

| Процедура | Призначення |
|---|---|
| `usp_CreateSwap` | створення заявки в одній транзакції (Swaps, SwapDetails, SwapSkills, історія) |
| `usp_ChangeSwapStatus` | транзакційна зміна статусу з перевіркою прав, дозволених переходів і RowVersion; недопустимий перехід дає помилку 50012 |
| `usp_GetSwapById` | заявка з деталями та навичками |
| `usp_GetUserSwaps` | заявки користувача з фільтрами та пагінацією |
| `usp_SoftDeleteSwap` | м'яке видалення з перевіркою RowVersion |

Статуси: Pending, Accepted, Completed, Rejected, Cancelled.

### Sessions (`QPQ_Sessions`)

| Таблиця | Призначення | Зв'язок |
|---|---|---|
| SessionFormats | довідник форматів (Online, InPerson) | 1:N до Sessions |
| AcceptedSwaps | локальна копія прийнятих обмінів | 1:N до Sessions |
| Sessions | зустрічі в межах обміну | головна сутність |
| SessionOutcomes | підсумок проведеної зустрічі | 1:1 із Sessions |
| Skills | локальна копія навичок з Catalog | M:N із Sessions через SessionSkills |
| SessionSkills | навички, які розглядають на зустрічі, з полем `PlannedMinutes` | M:N з власним полем |

Довідник `SessionFormats` заповнюється в `seed.sql`. Статуси зустрічі: Planned, Completed, Cancelled.

#### Обмеження та індекси

| Тип | Приклади |
|---|---|
| UNIQUE | `UQ_SessionFormats_Code`, `UX_Skills_Name`, `UX_Sessions_Swap_Time` (не можна призначити дві зустрічі на один обмін в один час) |
| CHECK | `DurationMinutes > 0`, `PlannedMinutes > 0`, допустимі статуси, `CK_Sessions_Place` (для Online потрібне посилання, для InPerson місце) |
| DEFAULT | `Status = 'Planned'`, `DurationMinutes = 60`, `PlannedMinutes = 30` |
| Каскади | `NO ACTION` у всіх FK, зустрічі видаляються м'яко |

| Індекс | Під який запит створено |
|---|---|
| `IX_AcceptedSwaps_Initiator`, `IX_AcceptedSwaps_Partner` | пошук обмінів користувача для перевірки прав доступу до зустрічей |
| `IX_Sessions_Swap_ScheduledAt` | перелік зустрічей обміну за часом, лише активні |
| `UX_Sessions_Swap_Time` | заборона дублювання часу зустрічі в межах обміну |
| `IX_SessionSkills_Skill` | пошук зустрічей за навичкою |

### Messages (`QPQ_Messages`)

| Колекція | Призначення | Підхід |
|---|---|---|
| conversations | розмова за прийнятим обміном | вбудовані `participants` (масив піддокументів) і `lastMessage` (гібрид: знімок посилання); посилання на обмін через `swapId`; денормалізоване `swapStatus` |
| messages | повідомлення розмови | посилання на `conversationId`; три типи документів із різними наборами полів: `text`, `file`, `system` |
| attachments | файли розмови | окрема колекція, на яку посилаються повідомлення типу `file` через `attachments[].attachmentId` |

У повідомленнях типу `file` вкладення зберігаються гібридно: посилання `attachmentId` плюс вбудована назва `fileName`. Структуру документів перевіряє `$jsonSchema` (для `messages` через `oneOf` за типом): формат GUID, допустимі статуси та події, довжина тексту, не більше десяти вкладень.

| Індекс | Під який запит створено |
|---|---|
| `ux_conversations_swap` (унікальний) | одна розмова на обмін, пошук розмови за `swapId` |
| `ix_conversations_participant` | список розмов користувача |
| `ix_messages_conversation_createdAt` | хронологічна стрічка повідомлень розмови, без видалених |
| `ix_messages_sender_createdAt` | повідомлення конкретного відправника |
| `ix_attachments_conversation_uploadedAt` | файли розмови від нових до старих |

## Swaps API (Лабораторна 2)

Мікросервіс Swaps реалізовано як вертикаль від бази `QPQ_Swaps` до API: Api → Bll → Dal, спільні моделі й винятки в Domain. Доступ до даних на ADO.NET та Dapper, запуск разом із базою через .NET Aspire.

### Структура рішення

```
Platform.slnx
Platform.AppHost/            оркестрація Aspire (AppHost.cs)
Services/Swaps/
    Swaps.Api/               контролери, Program.cs, обробка помилок, Swagger
    Swaps.Bll/               сервіси, DTO, профілі AutoMapper
    Swaps.Dal/               репозиторії, Unit of Work
    Swaps.Domain/            моделі, константи, доменні винятки
db/                          SQL- і MongoDB-скрипти (Лаб. 1)
docs/                        ERD, контексти, приклади документів
```

```
HTTP -> Swaps.Api -> Swaps.Bll -> Swaps.Dal -> SQL Server (QPQ_Swaps)
            \____________ Swaps.Domain ____________/
```

Залежності спрямовані в один бік: контролер звертається лише до сервісу, SQL існує тільки в `Swaps.Dal` та в процедурах бази.

| Шар | Що містить |
|---|---|
| Api | `SwapsController`, `SkillsController`, `DomainExceptionHandler` (ProblemDetails), поточний користувач, Swagger, Serilog |
| Bll | `SwapService`, `SkillService`, DTO з DataAnnotations, `SwapProfile` (AutoMapper) |
| Dal | `IGenericRepository<T>`, `BaseDapperRepository<T>`, `SwapRepository`, `SkillRepository`, `SwapStatusHistoryRepository`, `UnitOfWork` |
| Domain | `Swap`, `SwapDetails`, `SwapSkill`, `Skill`, `SwapStatusHistoryEntry`, `NotFoundException`, `BusinessConflictException`, `ValidationException` |

#### Репозиторії

| Репозиторій | Реалізація | Особливості |
|---|---|---|
| `SwapStatusHistoryRepository` | чистий ADO.NET (`SqlCommand`, `SqlDataReader`), без Generic Repository | параметри з явними типами, ручне читання |
| `SwapRepository` | Dapper на `BaseDapperRepository<Swap>` | multi-mapping обміну з деталями й навичками одним запитом, виклики `usp_ChangeSwapStatus`, `usp_GetUserSwaps`, `usp_SoftDeleteSwap` |
| `SkillRepository` | Dapper на `BaseDapperRepository<Skill>` | вибірка за списком Id (`IN @Ids`) |

Спільний CRUD (`GetByIdAsync`, `GetAllAsync`, `AddAsync`, `DeleteAsync`) написано один раз у `BaseDapperRepository<T>`. Усі значення передаються лише параметрами, ім'я таблиці та списки колонок є константами репозиторіїв. З'єднання й команди звільняються через `await using`, з'єднання UoW закривається в `DisposeAsync`.

### Запуск через Aspire

Потрібно: .NET 10 SDK і Docker Desktop. Починаючи з Лаб. 05 AppHost сам піднімає контейнер SQL Server і створює базу `QPQ_Swaps` скриптами з `db/p1` (див. розділ «Лаб. 05 — запуск платформи»). Для запуску без Aspire потрібна LocalDB з базою, розгорнутою за кроками розділу «Проєкт №1 (Swaps)».

1. Задайте пароль SQL Server: `dotnet user-secrets set "Parameters:sql-password" "<пароль>" --project Platform.AppHost`.
2. Запустіть AppHost однією командою:

```
dotnet run --project Platform.AppHost
```

3. Відкрийте Aspire Dashboard за посиланням із консолі: ресурс `swaps-api` має статус Running.
4. Візьміть URL API з Dashboard і відкрийте `<URL>/swagger`.

Рядок підключення `SwapsDb` до бази в контейнері Aspire формує сам і передає в API через `WithReference`. Без Aspire сервіс запускається окремо (`dotnet run --project Services/Swaps/Swaps.Api`, Swagger на `http://localhost:5180/swagger`) і бере рядок з `Services/Swaps/Swaps.Api/appsettings.json`. Рядок можна перевизначити змінною оточення:

```
ConnectionStrings__SwapsDb=Server=...;Database=QPQ_Swaps;...
```

Ліцензійний ключ AutoMapper (Community-редакція з automapper.io) необов'язковий: без нього сервіс працює і лише пише попередження в лог. За потреби задайте його через `dotnet user-secrets` (`AutoMapper:LicenseKey`).

### Поточний користувач

Ідентифікатор користувача береться з токена (claim `sub`). Автентифікації в цій лабораторній ще немає, тому в середовищі Development працює заголовок `X-User-Id` з GUID користувача. У Swagger його задають один раз кнопкою Authorize. Без користувача API повертає 401.

Тестові користувачі з `db/p1/seed.sql`:

| Позначення | UserId | Обміни |
|---|---|---|
| U1 | `3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01` | 1 (ініціатор, Pending), 3 (Completed), 4 (Rejected) |
| U2 | `7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02` | 1 (партнер), 2 (ініціатор, Accepted), 5 (Cancelled) |
| U3 | `b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03` | 2 (партнер), 3 (ініціатор) |
| U4 | `e8d05b44-9f7a-4c21-a6b3-1d2e4f7a8b04` | 4 (ініціатор), 5 (партнер) |

Користувач бачить лише обміни, у яких він учасник. Чужий обмін повертає 404.

### Ендпоінти

| Метод і шлях | Успіх | Помилки |
|---|---|---|
| `GET /api/swaps?role=&status=&page=&pageSize=` | 200 | 400, 401 |
| `GET /api/swaps/{id}` | 200 | 401, 404 |
| `POST /api/swaps` | 201 + Location | 400, 401, 404 (навичку не знайдено) |
| `PUT /api/swaps/{id}/details` | 200 | 400, 404, 409 |
| `DELETE /api/swaps/{id}` | 204 | 404, 409 |
| `POST /api/swaps/{id}/accept`, `/reject`, `/cancel`, `/complete` | 204 | 404, 409 |
| `GET /api/swaps/{id}/history` | 200 | 404 |
| `GET /api/skills`, `GET /api/skills/{id}` | 200 | 404 |

Дії зі зміною статусу приймають необов'язкове тіло `{ "rowVersion": "...", "comment": "..." }`. Якщо `rowVersion` не передано, береться актуальна версія обміну. Якщо передано застарілу, повертається 409. Поле `rowVersion` повертається в кожній відповіді з обміном.

### Помилки у форматі ProblemDetails

Усі винятки перетворює один глобальний обробник `DomainExceptionHandler`, контролери не містять `try/catch`. Некоректне тіло запиту (DataAnnotations) відхиляється ще до виклику сервісу з кодом 400.

| Виняток | Статус | Приклад |
|---|---|---|
| `NotFoundException` | 404 | обмін не існує або користувач не є його учасником |
| `BusinessConflictException` | 409 | недопустимий перехід статусу, застаріла `rowVersion` |
| `ValidationException` | 400 | співпадають пропонована й запитувана навичка |
| `UnauthorizedAccessException` | 401 | немає користувача |
| інші | 500 | деталі не передаються клієнту, помилка пишеться в лог |

Помилки процедур перетворюються в шарі Dal (`SqlExceptionTranslator`): 50010 → NotFound, 50011 і 50012 → BusinessConflict, 50003 і 50004 → Validation, порушення UNIQUE (2601, 2627) → BusinessConflict.

### Транзакції та рівень ізоляції

Створення обміну (`SwapService.CreateAsync`) змінює чотири таблиці через три репозиторії в одній транзакції Unit of Work:

1. `SwapRepository`: вставка `Swaps`, `SwapDetails`;
2. `SkillRepository`: перевірка, що обидві навички існують;
3. `SwapRepository`: вставка `SwapSkills`;
4. `SwapStatusHistoryRepository`: запис «Swap created» в історію.

Якщо навичку не знайдено, `NotFoundException` відкочує вже вставлені `Swaps` і `SwapDetails`. Зміна статусу виконується процедурою `usp_ChangeSwapStatus`, яка має власну транзакцію, перевіряє права, допустимий перехід і `RowVersion`, а також пише історію.

Рівень ізоляції Unit of Work за замовчуванням `ReadCommitted`: він не дозволяє читати незафіксовані зміни й не створює зайвих блокувань. Вищий рівень (`RepeatableRead`, `Serializable`) зменшує аномалії читання, але збільшує кількість блокувань і ризик взаємних блокувань, тому для звичайних операцій він не потрібен. Конкурентні зміни одного обміну захищені окремо: оптимістично через `RowVersion` та песимістично через `UPDLOCK, HOLDLOCK` у процедурі зміни статусу. Таймаут команд залишено за замовчуванням SqlClient (30 секунд).

### Приклади запитів

Нижче `BASE` це URL API з Aspire Dashboard (наприклад, `http://localhost:5180`), а `U1`, `U2` взято з таблиці користувачів. Команди написано для bash (Git Bash). У PowerShell замість `curl` використовуйте `curl.exe`, а запити з тілом зручніше виконувати з файлу `Services/Swaps/Swaps.Api/Swaps.Api.http` у Visual Studio або через Swagger.

```
BASE=http://localhost:5180
U1=3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01
U2=7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02
```

Список обмінів користувача та один обмін:

```
curl -i "$BASE/api/swaps" -H "X-User-Id: $U1"
curl -i "$BASE/api/swaps/1" -H "X-User-Id: $U1"
```

Створення обміну (201 і заголовок `Location`), у повідомленні є спецсимвол:

```
curl -i -X POST "$BASE/api/swaps" -H "X-User-Id: $U1" -H "Content-Type: application/json" \
  -d '{"partnerId":"7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02","offeredSkillId":1,"requestedSkillId":2,"message":"O'\''Brien: C# за англійську","durationMinutes":60}'
```

Партнер приймає обмін 1 (204), повторне прийняття дає 409 у форматі ProblemDetails:

```
curl -i -X POST "$BASE/api/swaps/1/accept" -H "X-User-Id: $U2"
curl -i -X POST "$BASE/api/swaps/1/accept" -H "X-User-Id: $U2"
```

Неіснуючий обмін дає 404, а не 500:

```
curl -i "$BASE/api/swaps/9999" -H "X-User-Id: $U1"
```

Відкат транзакції: навичка 999 не існує, відповідь 404, і в таблиці `Swaps` не з'являється нового рядка (перевірте `SELECT COUNT(*) FROM dbo.Swaps` до й після запиту):

```
curl -i -X POST "$BASE/api/swaps" -H "X-User-Id: $U1" -H "Content-Type: application/json" \
  -d '{"partnerId":"7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02","offeredSkillId":1,"requestedSkillId":999}'
```

Помилка валідації (400, `durationMinutes` поза діапазоном) і запит без користувача (401):

```
curl -i -X POST "$BASE/api/swaps" -H "X-User-Id: $U1" -H "Content-Type: application/json" \
  -d '{"partnerId":"7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02","offeredSkillId":1,"requestedSkillId":2,"durationMinutes":0}'
curl -i "$BASE/api/swaps"
```

Історія статусів обміну:

```
curl -i "$BASE/api/swaps/1/history" -H "X-User-Id: $U1"
```

Приклад відповіді 409:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Business rule conflict",
  "status": 409,
  "detail": "This status transition is not allowed for this user.",
  "traceId": "0HN7..."
}
```

## Sessions API (Лабораторна 3)

Мікросервіс Sessions (Проєкт №2) реалізовано на EF Core за підходом Code-First: схему бази створюють міграції з моделей, а не SQL-скрипти. Сервіс працює з окремою базою `QPQ_SessionsEf`, щоб не конфліктувати з базою `QPQ_Sessions`, яку створюють скрипти `db/p2` з Лаб. 1.

### Структура

```
Services/Sessions/
    Sessions.Api/        контролери, Program.cs, обробка помилок, Serilog, Swagger
    Sessions.Bll/        сервіси, DTO, профіль AutoMapper, специфікації, валідатори
    Sessions.Dal/        DbContext, Configurations/, Migrations/, репозиторії, Unit of Work, DbInitializer
    Sessions.Domain/     сутності з бізнес-правилами, константи, доменні винятки
```

```
HTTP -> Sessions.Api -> Sessions.Bll -> Sessions.Dal (EF Core) -> SQL Server (QPQ_SessionsEf)
```

Контролери працюють лише із сервісами, сервіси працюють лише з `IUnitOfWork`, а `DbContext` використовується тільки в `Sessions.Dal`.

### Модель і Fluent API

| Сутність | Таблиця | Зв'язок | Правила в сутності |
|---|---|---|---|
| `SessionFormat` | SessionFormats | 1:N до Sessions | код і назва обов'язкові |
| `Skill` | Skills | M:N із Sessions через SessionSkills | назва до 100 символів |
| `AcceptedSwap` | AcceptedSwaps | 1:N до Sessions | учасники різні; зустрічі можна планувати лише для обміну в статусі Accepted |
| `Session` | Sessions | головна сутність | тривалість 1–1440 хв; Online вимагає посилання, InPerson місце; змінювати можна лише Planned; сума `PlannedMinutes` навичок не перевищує тривалість; завершену зустріч не можна видалити |
| `SessionSkill` | SessionSkills | проміжна сутність M:N з полями `PlannedMinutes`, `AddedAt` | хвилини > 0, навичка не повторюється |
| `SessionOutcome` | SessionOutcomes | 1:1 із Sessions (PK = FK) | фактична тривалість > 0 |

Властивості сутностей мають `private set`, стан змінюється лише методами (`Reschedule`, `ChangePlace`, `AddSkill`, `RemoveSkill`, `Complete`, `Cancel`, `Delete`). Порушення правила дає `DomainException`.

Кожна сутність має свій клас `IEntityTypeConfiguration<T>` у `Sessions.Dal/Configurations`. Усі вони підключаються одним рядком `ApplyConfigurationsFromAssembly` в `SessionsDbContext.OnModelCreating`. У конфігураціях задано:

- назви таблиць, ключі (складений `SessionSkills (SessionId, SkillId)`), довжини рядків, `tinyint` для формату, `rowversion`;
- CHECK: `CK_Sessions_Duration`, `CK_Sessions_Status`, `CK_Sessions_Place`, `CK_SessionSkills_Minutes`, `CK_SessionOutcomes_Duration`, `CK_AcceptedSwaps_Status`, `CK_AcceptedSwaps_Parties`;
- унікальні індекси: `UQ_SessionFormats_Code`, `UX_Skills_Name` і `UX_Sessions_Swap_Time` (фільтр `[IsDeleted] = 0`);
- м'яке видалення зустрічей через глобальний фільтр `HasQueryFilter(s => !s.IsDeleted)`;
- `RowVersion` як маркер оптимістичної конкурентності.

У базі всі FK мають `NO ACTION`. Для `SessionSkills` і `SessionOutcomes` задано `ClientCascade`: коли навичку прибирають із колекції зустрічі, EF видаляє рядок сам, а каскаду на рівні БД немає.

### Міграції

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project Services/Sessions/Sessions.Dal --startup-project Services/Sessions/Sessions.Api --output-dir Migrations
dotnet ef database update --project Services/Sessions/Sessions.Dal --startup-project Services/Sessions/Sessions.Api -- --environment Development
dotnet ef migrations script --idempotent --project Services/Sessions/Sessions.Dal --startup-project Services/Sessions/Sessions.Api --output Services/Sessions/Sessions.Dal/Migrations/InitialCreate.sql
```

Міграція `InitialCreate` лежить у `Sessions.Dal/Migrations`, згенерований ідемпотентний SQL-скрипт у `Sessions.Dal/Migrations/InitialCreate.sql`. Окремо застосовувати міграцію не обов'язково: під час старту сервіс сам викликає `MigrateAsync()`.

### Початкові дані

| Режим | Де | Що |
|---|---|---|
| модельний | `HasData` у `SessionFormatConfiguration`, `SkillConfiguration` | довідник форматів (1 Online, 2 InPerson) і навички 1–6 з фіксованими Id, потрапляють у міграцію |
| програмний | `DbInitializer.SeedAsync` після `MigrateAsync()` у `Program.cs` | обміни 2 (U2 → U3, Accepted) і 3 (U3 → U1, Completed), зустрічі 1–3 з навичками, підсумок зустрічі 3 |

`DbInitializer` перед вставкою перевіряє наявність записів, тому повторний запуск не створює дублікатів. У середовищі Production програмний сідінг не виконується.

### Репозиторії та Unit of Work

`GenericRepository<T>` реалізує `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `Delete`, а також `ListAsync` і `CountAsync` за специфікацією через `DbSet<T>`. Збереження змін винесено в `UnitOfWork.SaveChangesAsync()`: його викликають один раз наприкінці бізнес-операції, тому всі зміни записуються однією транзакцією.

| Репозиторій | Спосіб завантаження | Метод |
|---|---|---|
| `SessionRepository` | Eager Loading: `Include` / `ThenInclude`, один SQL-запит | `GetWithDetailsAsync`: зустріч з обміном, форматом, підсумком і навичками |
| `AcceptedSwapRepository` | Explicit Loading: `Entry().Collection().Query().LoadAsync()`, два SQL-запити | `GetWithSessionsAsync`: спочатку обмін, потім його зустрічі |
| `SessionSkillRepository` | LINQ to Entities через проміжну сутність M:N | `GetBySkillForUserAsync`: зустрічі користувача з певною навичкою разом із `PlannedMinutes` і `AddedAt` |

`UnitOfWork` перетворює `DbUpdateConcurrencyException`, порушення UNIQUE (2601, 2627) і обмежень (547) на `BusinessConflictException`, тобто на відповідь 409.

### Специфікації

Складні вибірки описано класами Ardalis.Specification у `Sessions.Bll/Specifications` і виконуються одним методом репозиторію через `WithSpecification()`:

| Специфікація | Призначення |
|---|---|
| `SessionsFilterSpec` | фільтри, сортування та пагінація списку зустрічей |
| `UpcomingSessionsSpec` | найближчі заплановані зустрічі користувача |
| `AcceptedSwapsOfUserSpec` | обміни, у яких користувач є учасником |
| `SkillsByIdsSpec` | перевірка, що всі навички із запиту існують |

### Пагінація, фільтрація, сортування

`GET /api/sessions` приймає параметри:

| Параметр | Значення |
|---|---|
| `page` | номер сторінки з 1 |
| `pageSize` | 1–100, за замовчуванням 10 |
| `swapId`, `status`, `formatId`, `skillId` | фільтри |
| `from`, `to` | діапазон `scheduledAt` |
| `search` | пошук у `location` і `link` |
| `sortBy` | `scheduledAt` (за замовчуванням), `duration`, `status`, `createdAt` |
| `sortDir` | `asc` або `desc` |

Метадані повертаються в тілі відповіді:

```json
{ "items": [ ... ], "totalCount": 3, "page": 2, "pageSize": 1, "totalPages": 3, "hasNext": true, "hasPrevious": true }
```

Фільтри, `ORDER BY`, `OFFSET/FETCH` і `COUNT(*)` виконує SQL Server. У пам'ять потрапляє лише потрібна сторінка, це видно в лозі SQL-команд.

### Валідація та помилки

Прості правила задано атрибутами DataAnnotations на DTO (`Range`, `StringLength`, `Required`). Якщо вони не виконані, `[ApiController]` повертає 400 ще до виклику контролера. Правила, що залежать від кількох полів, описано у валідаторах FluentValidation (`Sessions.Bll/Validation`):

- для формату Online потрібне посилання http(s) і не допускається місце, для InPerson навпаки;
- час зустрічі має бути в майбутньому;
- навички не повторюються, а їхні хвилини не перевищують тривалість;
- допустимі `status`, `sortBy`, `sortDir` і `from <= to`.

Валідатори реєструються через `AddValidatorsFromAssemblyContaining` і викликаються в контролері явно. В обох випадках клієнт отримує `ValidationProblemDetails` з помилками по полях.

Винятки перетворює `DomainExceptionHandler` (`IExceptionHandler`):

| Виняток | Статус |
|---|---|
| `NotFoundException` | 404 |
| `ValidationException` | 400 |
| `BusinessConflictException`, `DomainException` | 409 |
| `UnauthorizedAccessException` | 401 |
| інші | 500 (деталі винятку лише в Development) |

Кожен виняток логується один раз, в обробнику.

### Логування

Serilog налаштовано в `Services/Sessions/Sessions.Api/appsettings.json` (розділ `Serilog`), вивід у консоль. `LogContextMiddleware` додає до кожного запису властивості `CorrelationId` (заголовок `X-Correlation-Id` або TraceIdentifier, повертається у відповіді), `UserId` і `RequestPath`. Рівень `Microsoft.EntityFrameworkCore.Database.Command` дорівнює `Information`, тому SQL-запити EF видно в лозі. У Development додатково увімкнено `EnableSensitiveDataLogging` для значень параметрів. Рівні можна змінити змінними оточення, наприклад `Serilog__MinimumLevel__Override__Microsoft.EntityFrameworkCore.Database.Command=Warning`.

### Запуск

Потрібно: .NET 10 SDK і Docker Desktop (для запуску без Aspire — SQL Server LocalDB). Базу створювати не потрібно, її створять міграції.

```
dotnet run --project Platform.AppHost
```

В Aspire Dashboard з'явиться ресурс `sessions-api`. Рядок підключення `SessionsDb` до бази в контейнері SQL Server Aspire передає через `WithReference`. Без Aspire сервіс запускається окремо:

```
dotnet run --project Services/Sessions/Sessions.Api
```

Swagger відкривається на `http://localhost:5190/swagger`, рядок підключення береться з `Services/Sessions/Sessions.Api/appsettings.json` або зі змінної `ConnectionStrings__SessionsDb`. Як і у Swaps, користувача в Development задають заголовком `X-User-Id`. Зустрічі обміну 2 бачать U2 і U3, зустріч 3 бачать U3 і U1.

### Ендпоінти

| Метод і шлях | Успіх | Помилки |
|---|---|---|
| `GET /api/sessions?page=&pageSize=&status=&formatId=&swapId=&skillId=&from=&to=&search=&sortBy=&sortDir=` | 200 | 400, 401 |
| `GET /api/sessions/upcoming?take=` | 200 | 400, 401 |
| `GET /api/sessions/{id}` | 200 | 401, 404 |
| `POST /api/sessions` | 201 + Location | 400, 404, 409 |
| `PUT /api/sessions/{id}` | 200 | 400, 404, 409 |
| `POST /api/sessions/{id}/complete` | 200 | 400, 404, 409 |
| `POST /api/sessions/{id}/cancel` | 204 | 404, 409 |
| `DELETE /api/sessions/{id}` | 204 | 404, 409 |
| `POST /api/sessions/{id}/skills` | 200 | 400, 404, 409 |
| `DELETE /api/sessions/{id}/skills/{skillId}` | 204 | 404, 409 |
| `GET /api/accepted-swaps?status=` | 200 | 400, 401 |
| `GET /api/accepted-swaps/{swapId}/sessions` | 200 | 401, 404 |
| `GET /api/skills` | 200 | |
| `GET /api/skills/{id}/sessions?from=` | 200 | 401, 404 |

Зміни зустрічі приймають необов'язкове поле `rowVersion` (Base64 з попередньої відповіді). Якщо його не передано, береться поточна версія, а застаріла версія дає 409.

### Приклади запитів

```
BASE=http://localhost:5190
U2=7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02
U3=b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03
```

Пагінація і сортування (друга сторінка, `totalCount` рахує всі зустрічі користувача):

```
curl -i "$BASE/api/sessions?page=2&pageSize=1&sortBy=scheduledAt&sortDir=desc" -H "X-User-Id: $U3"
```

Створення зустрічі (201 і `Location`), повторне створення на той самий час дає 409:

```
curl -i -X POST "$BASE/api/sessions" -H "X-User-Id: $U2" -H "Content-Type: application/json" \
  -d '{"swapId":2,"formatId":1,"scheduledAt":"2026-12-01T16:00:00Z","durationMinutes":60,"link":"https://meet.google.com/xyz","skills":[{"skillId":2,"plannedMinutes":30}]}'
```

Помилка валідації (400 з помилками по полях `Link`, `Location`, `ScheduledAt`):

```
curl -i -X POST "$BASE/api/sessions" -H "X-User-Id: $U2" -H "Content-Type: application/json" \
  -d '{"swapId":2,"formatId":2,"scheduledAt":"2020-01-01T10:00:00Z","durationMinutes":60,"link":"ftp://x"}'
```

Зустріч для завершеного обміну 3 (409) і неіснуюча зустріч (404):

```
curl -i -X POST "$BASE/api/sessions" -H "X-User-Id: $U3" -H "Content-Type: application/json" \
  -d '{"swapId":3,"formatId":1,"scheduledAt":"2026-12-01T16:00:00Z","durationMinutes":60,"link":"https://zoom.us/j/1"}'
curl -i "$BASE/api/sessions/9999" -H "X-User-Id: $U2"
```

Eager і Explicit Loading (у лозі один і два SQL-запити відповідно):

```
curl -i "$BASE/api/sessions/1" -H "X-User-Id: $U2"
curl -i "$BASE/api/accepted-swaps/2/sessions" -H "X-User-Id: $U2"
```

## Messages API (Лабораторна 4)

Мікросервіс Messages (Проєкт №3) побудовано за Clean Architecture з CQRS на MediatR. Він працює з MongoDB-базою `QPQ_Messages` з Лаб. 1: ті самі колекції, назви полів, індекси та початкові дані.

### Шари і залежності

```
Services/Messages/
    Messages.Domain/          Common (BaseEntity, ValueObject, Guard), Entities, ValueObjects, Enums, Constants, Exceptions, Interfaces
    Messages.Application/     Common (Behaviors, Interfaces, Mapping, Messaging, Models, Validation), Conversations/, Messages/, Attachments/
    Messages.Infrastructure/  Persistence (MongoDbContext, MongoMappings, Repositories, Queries), Seeding, HealthChecks
    Messages.WebApi/          Controllers, Contracts, Middleware, Program.cs, appsettings.*.json
```

```
HTTP -> Controller -> ISender -> LoggingBehavior -> ValidationBehavior -> PerformanceBehavior -> Handler
                                                                                                    |
                         Domain: IConversationRepository, IMessageRepository, IAttachmentRepository
                         Application: IMessageQueries
                                                                                                    |
                         Infrastructure: реалізації на MongoDB.Driver -> MongoDB (QPQ_Messages)
```

| Проєкт | Посилається на | Пакети |
|---|---|---|
| Messages.Domain | нічого | `MongoDB.Bson` (лише тип `ObjectId`) |
| Messages.Application | Domain | MediatR 12, FluentValidation, AutoMapper 15 |
| Messages.Infrastructure | Application, Domain | MongoDB.Driver, Options, Hosting, HealthChecks |
| Messages.WebApi | Application, Infrastructure (лише для `AddInfrastructure`) | Swashbuckle, Serilog.AspNetCore |

Кожен шар реєструє себе одним методом: `AddApplication` (MediatR, behaviors, валідатори, AutoMapper) і `AddInfrastructure` (MongoDB, репозиторії, запити, сідінг, індекси, health check). Контролери й обробники не бачать `IMongoCollection`: колекції використовуються лише в репозиторіях і `MessageQueries`.

### Домен

| Сутність | Колекція | Правила |
|---|---|---|
| `Conversation` | conversations | рівно два різні учасники (Initiator, Partner); статус обміну змінюється лише з Accepted на Completed або Cancelled; у скасовану розмову не можна писати; розмову активного обміну не можна видалити; `lastMessage` оновлюється разом із новим повідомленням |
| `Message` (абстрактна) | messages | `TextMessage` (текст 1–4000, редагування з `editedAt`), `FileMessage` (1–10 вкладень без повторів, підпис до 1000), `SystemMessage` (події SwapAccepted, SwapCompleted, SwapCancelled, не видаляється); змінювати й видаляти повідомлення може лише автор, видалення м'яке |
| `Attachment` | attachments | абсолютний http(s) URL до 500 символів, розмір 0–50 МБ, тип і назва файлу обов'язкові |

Усі сутності успадковують `BaseEntity` (`Id: ObjectId`, `CreatedAt`, `UpdatedAt`, `Version`), мають приватні setter-и та змінюють стан лише методами. Вкладені колекції (`participants`, `attachments`) зберігаються в приватних полях, назовні доступні тільки як `IReadOnlyCollection`. Порушення правил дає `DomainException`, відсутній документ `NotFoundException`, застаріла версія `ConflictException`.

Value Objects успадковують `ValueObject` (порівняння за `GetEqualityComponents`), незмінні та перевіряють дані в конструкторі: `Participant` (userId, role, lastReadAt), `LastMessage` (messageId, senderId, preview до 200 символів, sentAt), `AttachmentRef` (attachmentId, fileName), `SystemEventDetails` (swapId).

### Команди та запити

| Команда / запит | Тип | Що робить |
|---|---|---|
| `CreateConversationCommand` | `ICommand<ConversationDto>` | створює розмову й системне повідомлення SwapAccepted |
| `ChangeSwapStatusCommand` | `ICommand` | Completed або Cancelled + системне повідомлення, перевірка версії |
| `MarkConversationReadCommand` | `ICommand` | оновлює `lastReadAt` учасника |
| `DeleteConversationCommand` | `ICommand` | видаляє закриту розмову разом із повідомленнями й вкладеннями |
| `SendTextMessageCommand`, `SendFileMessageCommand` | `ICommand<MessageDto>` | нове повідомлення й оновлення `lastMessage` |
| `EditTextMessageCommand` | `ICommand` | редагування тексту з перевіркою версії |
| `DeleteMessageCommand` | `ICommand` | м'яке видалення, `lastMessage` перераховується |
| `UploadAttachmentCommand` | `ICommand<AttachmentDto>` | реєструє метадані файлу |
| `GetConversationByIdQuery`, `GetConversationsListQuery` | `IQuery<T>` | розмови поточного користувача |
| `GetMessageByIdQuery`, `GetMessagesListQuery` | `IQuery<T>` | стрічка повідомлень, фільтр за відправником, повнотекстовий пошук |
| `GetAttachmentByIdQuery`, `GetAttachmentsListQuery` | `IQuery<T>` | файли розмови |

Кожна операція лежить у власній папці (`Commands/<Назва>/` або `Queries/<Назва>/`) з трьома файлами: запит, обробник (`IRequestHandler`), валідатор. Обробники команд працюють з доменними сутностями через репозиторії та повертають DTO через AutoMapper (`ObjectId` → `string`, enum → `string`, VO → вкладені DTO). Обробники запитів викликають `IMessageQueries`, а він робить aggregation pipeline (`$match`, `$sort`, `$skip`, `$limit`, `$project`) з проєкцією одразу в DTO: `_id` і `conversationId` перетворюються на рядки засобами MongoDB (`$toString`), у застосунок потрапляє лише потрібна сторінка.

### Pipeline Behaviors і валідація

Порядок задано в `AddApplication` через `AddOpenBehavior`: `LoggingBehavior` (зовнішній), `ValidationBehavior`, `PerformanceBehavior` (біля обробника).

- `LoggingBehavior` пише структурований запис `Handling <Request> {...}` і `Handled <Request>`. Якщо запит відхилено валідацією, з'являється `rejected by validation, handler was not called` з переліком помилок.
- `ValidationBehavior` запускає всі валідатори запиту (`ValidateAsync`), збирає помилки разом і кидає `ValidationException`. Обробник у цьому разі не викликається.
- `PerformanceBehavior` вимірює час обробника і пише попередження `Slow request`, якщо він довший за 500 мс (у Development також Debug-запис з часом кожного запиту).

Валідатори FluentValidation є для всіх команд і запитів і реєструються `AddValidatorsFromAssembly`. Вони перевіряють формат ObjectId (`ObjectId.TryParse`), GUID користувачів, довжини, допустимі статуси, пагінацію (1–100) і значення `If-Match`. `SendFileMessageCommandValidator` має асинхронне правило з запитом до MongoDB: усі `attachmentIds` мають бути завантажені саме в цю розмову.

Приклад логу для некоректної команди (обробника в лозі немає):

```
[INF] Messages.Application.Common.Behaviors.LoggingBehavior
  Handling CreateConversationCommand {"UserId": "b19e6a33-...", "SwapId": 0, "InitiatorId": "x", ...}
[WRN] Messages.Application.Common.Behaviors.LoggingBehavior
  CreateConversationCommand rejected by validation, handler was not called: ["SwapId: 'Swap Id' must be greater than '0'.", ...]
```

### MongoDB: контекст, мапінг, конкурентність

`MongoDbContext` створює `MongoClient` з `IOptions<MongoDbOptions>` (секція `MongoDb`) і надає `IMongoCollection<T>` для `conversations`, `messages`, `attachments`. Репозиторії тримають колекцію в приватному полі.

Домен не містить BSON-атрибутів. Увесь мапінг описано в `MongoMappings.Register()` (викликається один раз в `AddInfrastructure`):

- конвенції camelCase, `IgnoreExtraElements`, enum як рядок;
- приватні поля `_participants` і `_attachments` мапляться в `participants` і `attachments`;
- Value Objects створюються через конструктор (`MapCreator`), тому валідація конструктора працює і під час читання;
- сутності створюються через приватний конструктор без параметрів;
- поліморфні повідомлення: дискримінатор `type` зі значеннями `text`, `file`, `system`, як у `$jsonSchema` з Лаб. 1;
- `updatedAt` пишеться лише в conversations, а вкладення зберігають `CreatedAt` як `uploadedAt` і не мають `version`, щоб документи проходили суворий валідатор колекцій.

Оптимістична конкурентність: кожне оновлення виконується `ReplaceOne` з фільтром `{ _id, version }` і збільшує `version`. Якщо документ за цей час змінили, `MatchedCount = 0` і повертається 409. Документи з `seed.js` не мають поля `version`, для них очікувана версія 0 означає «поле відсутнє або 0». У `db/p3/collections.js` до валідаторів conversations і messages додано необов'язкове поле `version` (int).

GET розмови та повідомлення повертає версію в заголовку `ETag` (наприклад `"2"`). Команди зміни приймають її в `If-Match`: якщо версія не збігається, відповідь 409 ще до запису. Без `If-Match` спрацьовує лише перевірка під час `ReplaceOne`.

### Індекси

`IndexCreationService` (`IHostedService`) під час старту створює індекси з тими самими назвами й параметрами, що в `collections.js`, тому повторне створення нічого не змінює:

| Індекс | Колекція | Ключі | Параметри |
|---|---|---|---|
| `ux_conversations_swap` | conversations | `swapId: 1` | unique |
| `ix_conversations_participant` | conversations | `participants.userId: 1` | |
| `ix_messages_conversation_createdAt` | messages | `conversationId: 1, createdAt: 1` | partial `isDeleted: false` |
| `ix_messages_sender_createdAt` | messages | `senderId: 1, createdAt: -1` | |
| `tx_messages_content` | messages | text по `text`, `caption` | `default_language: none` |
| `ix_attachments_conversation_uploadedAt` | attachments | `conversationId: 1, uploadedAt: -1` | |

### Початкові дані

`IDataSeeder` реалізують `ConversationSeeder`, `AttachmentSeeder`, `MessageSeeder`. Дані лежать у `Messages.Infrastructure/Seeding/Data/*.json` (Extended JSON, ті самі `_id` і значення, що в `db/p3/seed.js`) і вбудовані в збірку. Сідер шукає, яких `_id` ще немає в колекції, і вставляє лише їх одним `InsertManyAsync`, тому повторний запуск не створює дублікатів:

```
[INF] Seed data for conversations is already present (2 documents)
```

Сідінг вимикається параметром `MongoDb:SeedData` (у `appsettings.Production.json` він `false`).

### Конфігурація

| Файл | `MongoDb:ConnectionString` | `DatabaseName` |
|---|---|---|
| `appsettings.json` | `mongodb://localhost:27017/?maxPoolSize=50&serverSelectionTimeoutMS=5000&connectTimeoutMS=5000` | `QPQ_Messages` |
| `appsettings.Development.json` | localhost, `maxPoolSize=20`, таймаути 3 с | `QPQ_Messages` |
| `appsettings.Staging.json` | `mongodb://mongo-staging:27017/...` | `QPQ_Messages_Staging` |
| `appsettings.Production.json` | `mongodb://mongo:27017/...&retryWrites=true` | `QPQ_Messages` |

Розмір пулу й таймаути задаються в рядку підключення. Якщо є рядок `ConnectionStrings:MessagesDb` (його передає Aspire), він має пріоритет над `MongoDb:ConnectionString`. Стан підключення показує `GET /health` (команда `ping`).

### Запуск

Потрібно: .NET 10 SDK і MongoDB на `localhost:27017`. Скрипти Лаб. 1 запускати не обов'язково: сервіс сам створить індекси й початкові дані. Для суворої перевірки схеми спочатку виконайте `mongosh db\p3\collections.js`.

```
dotnet run --project Platform.AppHost
```

В Aspire Dashboard з'явиться ресурс `messages-api`, рядок `MessagesDb` до бази в контейнері MongoDB Aspire передає через `WithReference`. Окремо сервіс запускається так:

```
dotnet run --project Services/Messages/Messages.WebApi
```

Swagger: `http://localhost:5200/swagger`. Користувача в Development задають заголовком `X-User-Id` (кнопка Authorize у Swagger). Розмову 1 (`670000000000000000000c01`, обмін 2) бачать U2 і U3, розмову 2 (`670000000000000000000c02`, обмін 3) бачать U3 і U1. Готові запити є у `Services/Messages/Messages.WebApi/Messages.WebApi.http`.

### Ендпоінти

| Метод і шлях | Успіх | Помилки |
|---|---|---|
| `GET /api/conversations?swapStatus=&page=&pageSize=` | 200 | 400, 401 |
| `GET /api/conversations/{id}` | 200 + ETag | 400, 404 |
| `POST /api/conversations` | 201 + Location | 400, 409 (розмова для обміну вже є) |
| `PUT /api/conversations/{id}/status` | 204 | 400, 404, 409 |
| `POST /api/conversations/{id}/read` | 204 | 400, 404, 409 |
| `DELETE /api/conversations/{id}` | 204 | 400, 404, 409 |
| `GET /api/conversations/{id}/messages?search=&senderId=&page=&pageSize=` | 200 | 400, 404 |
| `POST /api/conversations/{id}/messages/text` | 201 + Location | 400, 404, 409 |
| `POST /api/conversations/{id}/messages/file` | 201 + Location | 400, 404, 409 |
| `GET /api/messages/{id}` | 200 + ETag | 400, 404 |
| `PUT /api/messages/{id}` | 204 | 400, 404, 409 |
| `DELETE /api/messages/{id}` | 204 | 400, 404, 409 |
| `GET /api/conversations/{id}/attachments?page=&pageSize=` | 200 | 400, 404 |
| `POST /api/conversations/{id}/attachments` | 201 + Location | 400, 404 |
| `GET /api/attachments/{id}` | 200 | 400, 404 |
| `GET /health` | 200 | 503 |

`Location` містить рядковий ObjectId, наприклад `/api/messages/6ade2d767e08d9f0aef773f0`.

### Помилки

`ApiExceptionHandler` (`IExceptionHandler` + `AddProblemDetails`) повертає ProblemDetails з `traceId`, для валідації `ValidationProblemDetails` з помилками по полях:

| Виняток | Статус |
|---|---|
| `NotFoundException` | 404 |
| `ValidationException` (FluentValidation) | 400 |
| `DomainException` | 400 |
| `ConflictException` (версія) | 409 |
| `MongoWriteException` з DuplicateKey | 409 (у `detail` назва індексу) |
| інший `MongoWriteException` | 400 |
| `MongoConnectionException`, `TimeoutException` (сервер MongoDB недоступний) | 503 |
| `UnauthorizedAccessException` | 401 |
| інші | 500 (деталі лише в Development) |

### Демонстрація

```
BASE=http://localhost:5200
U1=3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01
U2=7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02
U3=b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03
```

Помилка валідації до виклику обробника (400, у лозі `rejected by validation, handler was not called`):

```
curl -i -X POST "$BASE/api/conversations" -H "X-User-Id: $U3" -H "Content-Type: application/json" \
  -d '{"swapId":0,"initiatorId":"x","partnerId":"3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01"}'
```

Дублікат за унікальним індексом: перший запит створює розмову для обміну 10 (201 + Location), повторний дає 409 з назвою індексу `ux_conversations_swap`:

```
curl -i -X POST "$BASE/api/conversations" -H "X-User-Id: $U1" -H "Content-Type: application/json" \
  -d '{"swapId":10,"initiatorId":"3f2a9c10-6b1e-4d7a-9a51-0c8e5d2b7f01","partnerId":"7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02"}'
```

Конфлікт версій: перше редагування з `If-Match: "0"` дає 204, повторне з тією самою версією 409:

```
curl -i -X PUT "$BASE/api/messages/670000000000000000000a01" -H "X-User-Id: $U2" -H 'If-Match: "0"' \
  -H "Content-Type: application/json" -d '{"text":"Привіт! Коли зручно зустрітися?"}'
curl -i -X PUT "$BASE/api/messages/670000000000000000000a01" -H "X-User-Id: $U2" -H 'If-Match: "0"' \
  -H "Content-Type: application/json" -d '{"text":"Ще одна правка"}'
```

Асинхронна валідація з запитом до MongoDB (вкладення з іншої розмови, 400) і неіснуючий документ (404):

```
curl -i -X POST "$BASE/api/conversations/670000000000000000000c01/messages/file" -H "X-User-Id: $U3" \
  -H "Content-Type: application/json" -d '{"attachmentIds":["670000000000000000000b02"]}'
curl -i "$BASE/api/conversations/ffffffffffffffffffffffff" -H "X-User-Id: $U2"
```

Перевірка в mongosh: документи мають структуру з Лаб. 1 (camelCase, вкладені `participants`, `lastMessage`, `attachments`, без службових полів .NET), індекси створені:

```
use QPQ_Messages
db.conversations.findOne()
db.messages.find({ conversationId: ObjectId('670000000000000000000c01') })
db.messages.getIndexes()
```


## Лаб. 05 — запуск платформи

Усі сервіси піддомену Swaps запускаються однією командою через .NET Aspire. Docker-контейнери з базами піднімає AppHost, тому LocalDB і ручне розгортання баз не потрібні.

### Склад платформи

| Ресурс | Проєкт | Призначення |
|---|---|---|
| `sql` | контейнер SQL Server | бази `QPQ_Swaps` (рядок `SwapsDb`) і `QPQ_SessionsEf` (рядок `SessionsDb`) |
| `mongo` | контейнер MongoDB | база `QPQ_Messages` (рядок `MessagesDb`) |
| `swaps-api` | `Services/Swaps/Swaps.Api` | обміни, Dapper |
| `sessions-api` | `Services/Sessions/Sessions.Api` | сесії, EF Core |
| `messages-api` | `Services/Messages/Messages.WebApi` | розмови й повідомлення, MongoDB |
| `aggregator` | `Platform.Aggregator` | повна картка обміну |
| `gateway` | `Platform.ApiGateway` | єдина точка входу (YARP), порт 5000 |

`Platform.ServiceDefaults` підключається в кожен сервіс через `builder.AddServiceDefaults()` і дає Serilog (JSON у консоль), OpenTelemetry (логи, метрики, трасування з SQL і MongoDB spans), service discovery, resilience для HttpClient, health-ендпоінти (див. Лаб. 07), а також наскрізний `X-Correlation-Id`.

База `QPQ_Swaps` створюється під час першого запуску скриптами `db/p1/schema.sql`, `procedures.sql` і `seed.sql`. AppHost читає їх з диска і передає в `WithCreationScript`, Aspire виконує їх пакетами за розділювачем `GO`. Sessions застосовує міграції EF Core і початкові дані при старті, Messages створює індекси та сідить дані сам. Дані зберігаються в Docker volume, повторний запуск їх не дублює.

### Передумови

- .NET 10 SDK;
- запущений Docker Desktop.

### Запуск

Пароль адміністратора SQL Server задається один раз через user-secrets AppHost (не менше 8 символів, великі й малі літери, цифри):

```
dotnet user-secrets set "Parameters:sql-password" "<пароль>" --project Platform.AppHost
dotnet dev-certs https --trust
dotnet run --project Platform.AppHost --launch-profile https
```

Профіль `https` обов'язковий: gRPC між Aggregator і сервісами працює лише через HTTPS-ендпоінти. У консолі з'явиться посилання на Aspire Dashboard. Усі ресурси мають перейти в стан Running. Gateway доступний на `http://localhost:5000`.

### Маршрути Gateway

| Шлях | Сервіс |
|---|---|
| `/api/aggregator/**` | aggregator |
| `/api/swaps/**`, `/api/skills/**` | swaps-api |
| `/api/sessions/**`, `/api/accepted-swaps/**` | sessions-api |
| `/api/messages/**`, `/api/conversations/**`, `/api/attachments/**` | messages-api |

### Приклади

```bash
U2=7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02
curl -i http://localhost:5000/api/swaps/2 -H "X-User-Id: $U2"
curl -i http://localhost:5000/api/sessions/1 -H "X-User-Id: $U2"
curl -i http://localhost:5000/api/conversations -H "X-User-Id: $U2"
curl -i http://localhost:5000/api/messages/<id> -H "X-User-Id: $U2"
curl -i http://localhost:5000/api/aggregator/swaps/2/full -H "X-User-Id: $U2"
```

Кожна відповідь містить заголовок `X-Correlation-Id`. Його можна передати й у запиті (`-H "X-Correlation-Id: demo-1"`), тоді за цим значенням у Dashboard → Structured logs видно логи Gateway, Aggregator і всіх сервісів, що обробляли запит.

### Повна картка обміну

`GET /api/aggregator/swaps/{id}/full` отримує обмін зі swaps-api (`GetSwap`), а потім паралельно (`Task.WhenAll`) забирає сесії з sessions-api (`GetSessionsBySwap`) і розмову з повідомленнями з messages-api (`GetSwapConversation`). Усі виклики йдуть через gRPC, у метаданих передається `x-user-id` учасника обміну. Якщо обміну немає або користувач не є його учасником, відповідь 404.

```json
{
  "swap": { "id": 2, "status": "Accepted", "...": "..." },
  "sessions": [ { "id": 1, "swapId": 2, "...": "..." } ],
  "messages": [ { "id": "...", "type": "system", "...": "..." } ],
  "warnings": []
}
```

### Часткова відповідь

1. У Dashboard зупиніть ресурс `messages-api` (кнопка Stop).
2. Повторіть `curl -i http://localhost:5000/api/aggregator/swaps/2/full -H "X-User-Id: $U2"`.
3. Відповідь лишається 200: `messages` порожній, а в `warnings` з'являється `"messages-api is unavailable"`. В Aggregator записується попередження в лог, а така відповідь не кешується. Затримка не перевищує дедлайн виклику messages-api (5 с, див. Лаб. 07).
4. Запустіть `messages-api` знову (Start).

### Спостережуваність

- Structured logs: у кожного запису є `ServiceName`, `CorrelationId`, `EnvironmentName`, `MachineName`, `ThreadId`, `TraceId`, `SpanId`.
- Traces: агрегувальний запит видно як ланцюжок gateway → aggregator → gRPC-виклик swaps-api, далі паралельно sessions-api і messages-api зі spans SQL і MongoDB.
- Бізнес-події пишуться окремими spans і логами: `CreateSwap`, `ChangeSwapStatus` (Swaps.Bll), `CreateSession`, `RescheduleSession` (Sessions.Bll), `SendTextMessage`, `SendFileMessage` (Messages.Application). Тексти повідомлень у логи не потрапляють.

## Лаб. 06 — gRPC і кешування

Заміри HTTP і gRPC та ефект кешу: [docs/lab06-performance.md](docs/lab06-performance.md).

### Redis у платформі

AppHost піднімає ресурс `redis` (`AddRedis("redis").WithDataVolume()`) і передає його рядок підключення в `swaps-api` та `aggregator`. Адреса в `appsettings` не прописується.

### Що кешується

Кеш Swaps дворівневий: L1 — `IMemoryCache` процесу, L2 — Redis. Порядок читання: L1 → L2 → БД, значення з L2 повертається в L1. Код лежить у `Services/Swaps/Swaps.Bll/Caching/`, декоратори `CachedSkillService` і `CachedSwapService` обгортають наявні сервіси, тому кеш діє і для REST, і для gRPC.

| Дані | Ключ | TTL L1 | TTL L2 |
|---|---|---|---|
| Усі навички | `swaps:skills:all` | 5 хв | 30 хв |
| Навичка | `swaps:skill:{id}` | 5 хв | 30 хв |
| Картка обміну | `swaps:swap:{id}` | 30 с | 2 хв |
| Повна картка в Aggregator | `aggregator:swap:{id}:full:{userId}` | — | 30 с |

- **Картка обміну** спільна для обох учасників, тому перевірка учасника виконується після читання з кешу і сама не кешується.
- **Агрегатор** кешує лише повні відповіді (`warnings` порожній). Ключ містить користувача, бо відповідь залежить від учасника. Після зміни обміну зведена картка може бути застарілою до 30 с.
- **Інвалідація** синхронна, одразу після запису в БД: `CachedSwapService` видаляє `swaps:swap:{id}` з L1 і L2 при створенні, зміні деталей, видаленні та кожній зміні статусу. У лог пишеться `Cache invalidated {Key}, reason {Reason}`. API для зміни навичок у Swaps немає, тому їхні записи застарівають лише за TTL.
- **Збій Redis** не валить сервіс: помилка логується як Warning, запит іде в БД, а L2 пропускається на 30 с, щоб кожен запит не чекав тайм-ауту.
- `HybridCache` у .NET дає подібну схему готовою; тут дворівневий кеш реалізовано вручну.

### gRPC-контракти

| Контракт | Методи |
|---|---|
| `Services/Swaps/Swaps.Api/Protos/swaps.proto` | `SwapsService.GetSwap`, `SwapsService.GetSkillsByIds` |
| `Services/Sessions/Sessions.Api/Protos/sessions.proto` | `SessionsService.GetSessionsBySwap` |
| `Services/Messages/Messages.WebApi/Protos/messages.proto` | `MessagesService.GetSwapConversation` |

Сервіси в папках `Grpc/` — адаптери над наявною BLL і MediatR. Учасник передається в метаданих `x-user-id`, усі виклики логує `GrpcLoggingInterceptor`.

| Ситуація | Статус gRPC | Відповідь Aggregator |
|---|---|---|
| Обміну немає | `NotFound` | 404 |
| Користувач не учасник обміну | `PermissionDenied` (Swaps) | 404, щоб не розкривати існування обміну |
| Немає або некоректний `x-user-id` | `Unauthenticated` | 401 |
| Некоректний id | `InvalidArgument` | 400 |
| Sessions або Messages повернули `NotFound` | `NotFound` | 200, порожній список без `warnings` |
| Sessions або Messages недоступні | `Unavailable`, `DeadlineExceeded` | 200 із `warnings`, не кешується |

Викликати сервіс напряму можна через `grpcurl` (порт `swaps-api`: 7180; reflection вимкнено, тому контракт передається явно):

```bash
grpcurl -insecure -import-path Services/Swaps/Swaps.Api/Protos -proto swaps.proto \
  -H "x-user-id: 7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02" \
  -d '{"id": 2}' localhost:7180 swaps.SwapsService/GetSwap
```

### Як перевірити

1. **Кеш.** Двічі виконайте `curl http://localhost:5000/api/swaps/2 -H "X-User-Id: $U2"`. У trace другого запиту немає SQL-span, у логах є `Cache hit swaps:swap:2 at L1`.
2. **Інвалідація.** Змініть обмін (`PUT /api/swaps/{id}/details` або зміна статусу): наступний GET повертає нові дані, у логах є `Cache invalidated`.
3. **Збій Redis.** Зупиніть `redis` у Dashboard: сервіси відповідають 200, перший запит повільніший, у логах Warning. Потім запустіть `redis` знову.
4. **Часткова відповідь.** Зупиніть `sessions-api`: Aggregator повертає 200 із `warnings`, і повторний запит знову звертається до сервісів.
5. **Кеш Aggregator.** Повторіть запит повної картки протягом 30 с: у логах Aggregator є `Cache hit aggregator:swap:2:full:... at L2`.
6. **Метрики.** Dashboard → Metrics → `swaps-api` або `aggregator`: лічильники `cache.hits` і `cache.misses` з тегом `cache.level`, у `swaps-api` також `cache.invalidations`.

## Лаб. 07 — резильєнтність, метрики, health checks

### Критичність сервісів

Aggregator спершу обов'язково читає обмін зі `swaps-api`: без нього картку не зібрати, відповідь буде 404 або 503. Сесії й повідомлення читаються паралельно і можуть бути відсутні, тоді відповідь часткова з `warnings`. Тому, на відміну від методички (де критичний каталог), у нас **критичний `swaps-api`**, а `sessions-api` і `messages-api` некритичні. Повідомлення найменш важливі, тож для них Aggregator здається найшвидше.

### Політики клієнтів Aggregator

`AddStandardResilienceHandler()` у `ConfigureHttpClientDefaults` (ServiceDefaults) лишається обробником за замовчуванням для всіх HttpClient і gRPC-клієнтів. Клієнти Aggregator спершу знімають його (`RemoveAllResilienceHandlers()`), а потім додають власний (`AddStandardResilienceHandler().Configure(...)`), тож обробники не нашаровуються. Усі значення зібрано в одному місці: `Platform.Aggregator/Resilience/ResiliencePolicies.cs`.

| Клієнт | HTTP retry | gRPC MaxAttempts (backoff) | AttemptTimeout | TotalTimeout = дедлайн | CB FailureRatio | MinThroughput | Sampling | Break |
|---|---|---|---|---|---|---|---|---|
| `swaps-api` | 2, 0.5 с exp + jitter | 5 (0.5 → 4 с) | 3 с | 20 с | 0.3 | 5 | 30 с | 15 с |
| `sessions-api` | 1, 0.5 с exp + jitter | 3 (0.5 → 2 с) | 2 с | 8 с | 0.5 | 5 | 20 с | 15 с |
| `messages-api` | 1, 0.5 с exp + jitter | 2 (0.5 → 1 с) | 2 с | 5 с | 0.7 | 5 | 10 с | 10 с |

- **swaps-api:** без обміну картки немає, тому найбільше спроб і найдовший загальний час. Найнижчий `FailureRatio` (0.3) розмикає ланцюг раніше: якщо сервіс справді лежить, краще швидко віддати 503, ніж тримати запити.
- **sessions-api:** без сесій картка все одно буде, лише з попередженням. Тому менше спроб і коротший тайм-аут.
- **messages-api:** найменш важливі дані. Одна повторна спроба, 5 с на все, найтолерантніший `FailureRatio` (0.7): поодинокі збої не відрізають повідомлення від усіх користувачів.
- **Чому HTTP-retry малий (2/1/1).** Повтори є на двох рівнях: Polly-retry на рівні HTTP і `RetryPolicy` gRPC-каналу. Коли сервіс недоступний, вони множаться: при 5 HTTP-спробах × 5 gRPC-спробах вийшло б 25 звернень до одного сервісу. Тому основну роботу робить gRPC `RetryPolicy`, а HTTP-retry зменшено до мінімуму (Polly не дозволяє 0).
- **Чому потрібен gRPC `RetryPolicy`.** gRPC передає статус у трейлерах відповіді з HTTP 200, тому HTTP-обробник Polly помилку не бачить. Кожен gRPC-клієнт отримує `ServiceConfig` з `RetryPolicy`, і канал сам повторює `Unavailable` і `ResourceExhausted`. Статуси `NotFound`, `PermissionDenied`, `InvalidArgument`, `Unauthenticated` не повторюються: це відповідь сервісу, а не збій. Усі наші gRPC-методи лише читають дані, тому повтор безпечний.
- **Таймаути.** `TotalRequestTimeout` ≥ спроба × кількість спроб + затримки. Дедлайн gRPC-виклику в `FullSwapService` дорівнює `TotalTimeout` клієнта (замість фіксованих 10 с). Він обмежує сумарний час усіх повторів обох рівнів і не обрізає повтори одного рівня.
- **Circuit breaker.** `MinimumThroughput` = 5, щоб ланцюг можна було розімкнути на навчальному стенді. `SamplingDuration` ≥ 2 × `AttemptTimeout` (вимога валідації). Переходи логуються з назвою клієнта: `OnOpened` як Error (з `BreakDuration`), `OnHalfOpened` і `OnClosed` як Information. Логер береться з `ILoggerFactory` контейнера. Коли ланцюг розімкнений, `BrokenCircuitException` доходить до каналу як `Internal`, gRPC його не повторює, і запит відхиляється одразу.
- **Retry для небезпечних методів** вимкнено (`DisableForUnsafeHttpMethods`), але з винятком для gRPC. gRPC надсилає кожен виклик як POST, тож без винятку HTTP-retry для gRPC-клієнтів не спрацював би ніколи. Запити з `Content-Type: application/grpc` (а всі наші методи — читання) зберігають стандартні правила транзитних збоїв, решта небезпечних методів не повторюється (`AllowRetryOnlyForGrpcReads`).
- **Зупинений через Aspire сервіс** не відмовляє в з'єднанні: проксі Aspire приймає його й мовчить. Тому спроба завершується за `AttemptTimeout`. Це Polly-тайм-аут, а не статус `Unavailable`, тож у цьому випадку повторює HTTP-retry, а gRPC `RetryPolicy` спрацьовує, коли сервіс сам повертає `Unavailable` / `ResourceExhausted` або коли з'єднання розривається.

### Деградація при збої Redis

`TwoLevelCache` (Swaps) і `FullSwapCache` (Aggregator) перехоплюють `RedisConnectionException` і `RedisTimeoutException`. Вони пишуть Warning `Redis unavailable, falling back to database for {Key}` (в Aggregator: `... falling back to downstream services ...`) або `Redis unavailable, value for {Key} not cached` і пропускають Redis на 30 с. Інші помилки обробляє загальний fallback. Клієнт Redis не зважає на `CancellationToken`, а недоступний сервер може тримати виклик до свого тайм-ауту (понад 10 с) — довше за `AttemptTimeout` Aggregator-а. Тому кожна операція з Redis обмежена 1 с (`WaitAsync`); перевищення трактується як збій Redis. Запит ніколи не падає через Redis.

### Бізнес-метрики

Класи метрик — синглтони, створені через `IMeterFactory`. Виклики стоять у декораторах і pipeline behavior, тому BLL і обробники не змінювалися. Лічильник збільшується лише після успішної операції. Гістограма записує тривалість і для невдалих операцій.

| Meter | Інструмент | Тип, одиниця | Теги та значення |
|---|---|---|---|
| `Swaps.Api` | `swaps.created` | Counter, `{swap}` | `swap.status` = `Pending` |
| | `swaps.status_changed` | Counter, `{swap}` | `swap.status` = `Accepted` / `Rejected` / `Cancelled` / `Completed` |
| | `swaps.operation.duration` | Histogram, `ms` | `operation` = `create` / `accept` / `reject` / `cancel` / `complete` / `get_card`; `status` = `success` / `failure` |
| | `swaps.cache.entries` | ObservableGauge, `{entry}` | — (з `IMemoryCache.GetCurrentStatistics()`, без запитів до БД) |
| `Sessions.Api` | `sessions.created`, `sessions.rescheduled` | Counter, `{session}` | `session.format` = `Online` / `InPerson` |
| | `sessions.operation.duration` | Histogram, `ms` | `operation` = `create` / `reschedule` / `complete` / `cancel` / `get`; `status` |
| `Messages.WebApi` | `messages.sent` | Counter, `{message}` | `message.type` = `text` / `file` |
| | `messages.operation.duration` | Histogram, `ms` | `operation` = `send_text` / `send_file` / `get_swap_conversation`; `status` |
| `Polly` | `resilience.polly.strategy.events` | Counter | `event.name` = `OnRetry` / `OnCircuitOpened` / `OnCircuitHalfOpened` / `OnCircuitClosed` / `OnTimeout`, `pipeline.name` |

- Тегів з UserId, SwapId, CorrelationId чи часом немає: лише значення з малих фіксованих наборів.
- Межі кошиків гістограм: 1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 мс.
- Нормальні діапазони на локальному стенді:
  - `get_card` з кешу — одиниці мс, з БД — 10–50 мс;
  - запис (`create`, зміна статусу, `reschedule`, `send_*`) — 10–100 мс;
  - понад 500 мс — сигнал проблеми (так само, як поріг `PerformanceBehavior` у Messages).
- `swaps.cache.entries` у межах `SizeLimit` = 10 000. `failure` має бути рідкісним, переважно 404/409 від бізнес-правил.
- `Polly`-події з `event.name = OnRetry` у нормі відсутні.
- Усі meter-и, а також `Swaps.Cache` і `Aggregator.Cache`, підключено в `ServiceDefaults` через `AddMeter(...)`.

### Health checks

| Ендпоінт | Перевірки | Призначення |
|---|---|---|
| `/health/live` (і `/alive`) | лише `self` (тег `live`), без залежностей | процес живий; при збої — перезапуск |
| `/health/ready` | перевірки з тегом `ready`, JSON з деталями | чи можна слати трафік |
| `/health` | усі перевірки, JSON | використовує `WithHttpHealthCheck` в AppHost |

Ендпоінти доступні в усіх середовищах (Swagger/OpenAPI лишились лише в Development). Відповідь `/health/ready` — JSON з `status`, `totalDurationMs` і для кожної перевірки `name`, `status`, `durationMs`, `description`, `error` (лише повідомлення, без stack trace), `tags`. Healthy і Degraded віддають 200, Unhealthy — 503.

| Сервіс | Перевірка | Збій → статус |
|---|---|---|
| swaps-api | `swaps-db` (SQL Server, `SELECT 1`, 5 с) | Unhealthy |
| swaps-api | `redis-cache` (3 с) | **Degraded**: сервіс працює з БД |
| sessions-api | `sessions-db` (SQL Server, 5 с) | Unhealthy |
| messages-api | `messages-db` (MongoDB ping тим самим `IMongoClient`, що в `MongoDbContext`, 5 с) | Unhealthy |
| aggregator | `redis-cache` | Degraded |
| aggregator | `swaps-api` (gRPC Health, дедлайн 2 с) | **Unhealthy**: без обміну картки немає |
| aggregator | `sessions-api`, `messages-api` (gRPC Health) | **Degraded**: можлива часткова відповідь |

- Власні перевірки Redis з Aspire-інтеграції вимкнено (`DisableHealthChecks = true`), інакше вони перекрили б Degraded статусом Unhealthy.
- Кожен сервіс публікує стандартний `grpc.health.v1.Health` (`AddGrpcHealthChecks` + `MapGrpcHealthChecksService`). Статус `Serving` залежить лише від перевірок з тегом `database`. Перевірка Redis туди не входить: при зупиненому Redis вона триває довше за дедлайн проби (2 с), і swaps-api помилково виглядав би для Aggregator як Unhealthy.
- SQL-перевірки використовують рядок підключення з `Connect Timeout = 5`. SqlClient не перериває очікування входу за скасуванням, тому без цього `/health/ready` при зупиненому SQL Server відповідав би 15 с.
- Aggregator опитує його через `DownstreamGrpcHealthCheck` з окремими іменованими клієнтами `*-health`. На них resilience-обробник знято: проба має показувати поточний стан, а не повторювати запит.
- В AppHost усі п'ять проєктів мають `WithHttpHealthCheck("/health")`. Тепер `WaitFor` чекає Healthy, а не просто запуску процесу. Degraded відповідає 200, тому зупинений Redis старт не блокує.

### Як відтворити

```bash
U2=7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02
curl -k https://localhost:7180/health/ready     # swaps-api
curl -k https://localhost:7116/health/ready     # aggregator: redis + три downstream-сервіси
```

1. **Повтори.** Зупиніть `messages-api` і кілька разів запросіть `/api/aggregator/swaps/2/full` (перші 30 с може відповідати кеш Aggregator).
   - Перші запити дають 200 з `warnings` за 4–5 с: дві спроби по 2 с плюс затримка, у межах дедлайну 5 с.
   - У логах aggregator Warning категорії `Polly` (`Execution attempt`).
   - Dashboard → Metrics → aggregator → `resilience.polly.strategy.events`, фільтр `event.name = OnRetry`.
2. **Circuit breaker.** Уже на третьому запиті набирається 5 збоїв.
   - У логах з'являється Error `Circuit breaker for messages-api opened for 00:00:10`, у метриці — `OnCircuitOpened`.
   - Наступні запити отримують часткову відповідь за ~30 мс.
   - Через 10 с один запит пробує знову (`half-open`). Якщо сервіс ще лежить, ланцюг знову розмикається.
   - Запустіть `messages-api` (Start): після `BreakDuration` з'являться `half-open` і `closed`, а `warnings` зникнуть.
3. **Redis.** Зупиніть `redis`.
   - `/health/ready` swaps-api і aggregator повертає 200 і `"status": "Degraded"`.
   - Запити працюють, у логах `Redis unavailable, falling back to database`.
4. **SQL Server.** Зупиніть `sql`: `/health/live` swaps-api і sessions-api повертає 200, а `/health/ready` — 503 `Unhealthy`.
5. **Бізнес-метрики.** Створіть обмін (`POST /api/swaps`) і змініть статус (`POST /api/swaps/{id}/accept` тощо).
   - Dashboard → Metrics → swaps-api: `swaps.created`, `swaps.status_changed` (фільтр `swap.status`).
   - Там же `swaps.operation.duration` (розподіл за `operation`) і `swaps.cache.entries`.

## Лаб. 08 — події та RabbitMQ

### Що реплікується

Події реалізують політику дублювання з Лаб. 01 (розділ «Політика дублювання даних»): власник прийнятого обміну — Swaps, а Sessions (`AcceptedSwaps`, SQL Server, EF Core) і Messages (`conversations`, MongoDB) тримають свої копії й оновлюють їх лише за подіями. Жоден сервіс не ходить у Swaps по дані: кожна подія самодостатня.

| Подія | Routing key | Коли публікується | Sessions | Messages |
|---|---|---|---|---|
| `SwapAcceptedEvent` | `swap.accepted` | партнер прийняв обмін | створює рядок `AcceptedSwaps` | створює розмову з учасниками і системним повідомленням `SwapAccepted` |
| `SwapCompletedEvent` | `swap.completed` | прийнятий обмін завершено | `Status = Completed` | `swapStatus = Completed` + системне повідомлення |
| `SwapCancelledEvent` | `swap.cancelled` | скасовано **прийнятий** обмін (для `Pending` копій ще немає, подія не йде) | `Status = Cancelled` | `swapStatus = Cancelled` (розмова стає лише для читання) + системне повідомлення |

Кожна подія несе `SwapId`, `InitiatorId`, `PartnerId`, `Status`, час зміни (`AcceptedAt` / `CompletedAt` / `CancelledAt`) і `ChangedByUserId`. Учасники є в кожній події, тож копію можна створити з будь-якої з них, у будь-якому порядку надходження. Автор зміни потрібен для системного повідомлення в розмові.

`SkillChanged` (власник — Catalog) і `UserDeleted` (Identity) належать до інших піддоменів і тут не реалізовані.

**Контракти.** Події — незмінні `record`-и, успадковані від `IntegrationEvent` (`EventId`, `OccurredAt`, `CorrelationId`, `SchemaVersion`) з `Platform.EventBus`. Видавець тримає їх у `Swaps.Api/IntegrationEvents/Events/`, а кожен споживач має **власну копію** (`Sessions.Api/IntegrationEvents/Events/`, `Messages.WebApi/IntegrationEvents/Events/`), як у eShop. Сервіси не посилаються один на одного. Спільні лише формат (JSON camelCase), ім'я типу у властивості `Type` і версія схеми, тож споживач може змінюватись незалежно від видавця.

### Топологія

Оголошується в `Platform.EventBus/Topology.cs` при старті і видавцем, і кожним споживачем (повторне оголошення безпечне).

| Об'єкт | Тип | Параметри | Прив'язка |
|---|---|---|---|
| `swaps-events` | exchange, **topic** | durable | — |
| `sessions.swaps-sync` | queue | durable, `x-dead-letter-exchange = swaps-events.dlx`, `x-dead-letter-routing-key = sessions.swaps-sync` | до `swaps-events` за `swap.*` |
| `messages.swaps-sync` | queue | durable, `x-dead-letter-exchange = swaps-events.dlx`, `x-dead-letter-routing-key = messages.swaps-sync` | до `swaps-events` за `swap.*` |
| `swaps-events.dlx` | exchange, **direct** | durable | — |
| `sessions.swaps-sync.dlq` | queue | durable | до `swaps-events.dlx` за `sessions.swaps-sync` |
| `messages.swaps-sync.dlq` | queue | durable | до `swaps-events.dlx` за `messages.swaps-sync` |

- Кожен споживач має свою чергу, тож обидва отримують кожну подію.
- DLX саме **direct**: черга відправляє «мертве» повідомлення з routing key, що дорівнює її назві, і воно потрапляє лише в її DLQ. Fanout-DLX продублював би його в обидві.
- AppHost: `AddRabbitMQ("messaging").WithDataVolume().WithManagementPlugin()`, `WithReference(rabbit).WaitFor(rabbit)` для swaps-api, sessions-api, messages-api.
- У сервісах `AddRabbitMQClient("messaging")` реєструє `IConnection` (RabbitMQ.Client 7, лише асинхронний API).
- Власні health check і трасування інтеграції вимкнено, натомість:
  - перевірка `messaging` з тегом `ready` і статусом Degraded видно в `/health/ready`. Без брокера сервіс відповідає на запити, відстає лише реплікація;
  - використовуються власні spans `Platform.Messaging`.

### Формат повідомлення

| Властивість / заголовок | Значення |
|---|---|
| `delivery_mode` | 2 (`Persistent = true`) |
| `content_type` | `application/json` |
| `message_id` | `EventId` |
| `type` | ім'я класу події, наприклад `SwapAcceptedEvent`; за ним споживач обирає обробник |
| `timestamp` | `OccurredAt` (Unix-секунди) |
| `correlation_id`, `x-correlation-id` | `X-Correlation-Id` HTTP-запиту, що змінив обмін |
| `x-schema-version` | `1`; іншу версію споживач відправляє в DLQ |
| `traceparent`, `tracestate` | W3C trace context |
| `x-retry-count` | номер повтору (лише в повторно опублікованих повідомленнях) |

```json
{
  "swapId": 3002,
  "initiatorId": "7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02",
  "partnerId": "b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03",
  "status": "Accepted",
  "acceptedAt": "2026-11-20T19:55:26.7741807Z",
  "changedByUserId": "b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03",
  "eventId": "ab9588c6-ca6c-42aa-96fa-c33fdec90396",
  "occurredAt": "2026-11-20T19:55:26.7741842Z",
  "correlationId": "lab08-format-3002",
  "schemaVersion": 1
}
```

### Публікація

`Swaps.Api/IntegrationEvents/PublishingSwapService.cs` — декоратор `ISwapService`. Ланцюжок у DI (зовнішній першим): `MeteredSwapService → PublishingSwapService → CachedSwapService → SwapService`.

1. Спочатку виконується `inner.AcceptAsync / CompleteAsync / CancelAsync`. Якщо зміна в БД не вдалась, виняток іде далі, і подія не публікується.
2. Після успіху декоратор читає картку обміну. Кеш уже інвалідовано, тож це закомічений стан. Далі він публікує подію з `CorrelationId` запиту.

`RabbitMqEventPublisher`:
- працює через один `IChannel` під `SemaphoreSlim`, бо канал не потокобезпечний;
- має **publisher confirms**: `PublishAsync` завершується, лише коли брокер підтвердив прийом;
- відкриває Activity типу Producer `swaps-events publish`.

Після публікації в лог іде `Published {EventType} {EventId} to {Exchange}/{RoutingKey} for swap {SwapId}, correlation {CorrelationId}`.

**Outbox (не реалізовано).** Між commit у БД і публікацією є вікно: якщо брокер недоступний (тайм-аут публікації 5 с) або процес впаде саме тоді, зміна в Swaps залишиться, а подія загубиться. Тоді копії в Sessions і Messages розійдуться з джерелом. Зараз збій публікації лише логується як Error з бізнес-контекстом (`Failed to publish {EventType} for swap {SwapId}…`), а запит завершується успішно, бо зміна вже збережена. Надійне рішення — Transactional Outbox:
- таблиця `OutboxMessages` у `QPQ_Swaps`;
- подія записується в одній транзакції зі зміною статусу;
- фоновий сервіс читає непубліковані записи, публікує їх і позначає відправленими (at-least-once; дублікати споживачі вже відкидають).

Тут зміна статусу виконується збереженою процедурою поза транзакцією Unit of Work, тому outbox вимагав би переписати BLL/DAL Swaps. Це свідомо лишено поза межами лаби.

### Споживання, ack і повтори

`Platform.EventBus/RabbitMqConsumerBase.cs` — спільний `BackgroundService`. Конкретні `SwapEventsConsumer` у `Sessions.Api/IntegrationEvents/` і `Messages.WebApi/IntegrationEvents/` лише називають свою чергу й типи подій.

Запуск:
1. `CreateChannelAsync`;
2. `Topology.DeclareAsync`;
3. `BasicQosAsync(0, 10, false)`;
4. `AsyncEventingBasicConsumer` (`ReceivedAsync`);
5. `BasicConsumeAsync(autoAck: false)`.

Під час зупинки канал закривається, а повідомлення, що оброблялось, повертається в чергу (`nack requeue: true`). Кожне повідомлення обробляється у власному DI-scope окремим обробником: `SwapAcceptedHandler`, `SwapCompletedHandler`, `SwapCancelledHandler`.

| Результат | Дія | Лог |
|---|---|---|
| успіх або дублікат | `BasicAckAsync` | `Acked …` (і `Duplicate event {EventId} skipped`) |
| постійний збій: некоректний JSON, невідомий `Type`, непідтримувана `x-schema-version`, порушення доменних правил | `BasicNackAsync(requeue: false)` → DLX → DLQ своєї черги | Error `Dead-lettered … : {Reason}` |
| тимчасовий збій: БД недоступна, тайм-аут, інші винятки | пауза 1·2ⁿ с, повторна публікація того самого тіла з `x-retry-count + 1` **у ту саму чергу** (default exchange, routing key = назва черги), потім ack оригіналу | Warning `Retry {Attempt}/3 …` |
| тимчасовий збій, `x-retry-count` ≥ 3 | `BasicNackAsync(requeue: false)` → DLQ | Error `Dead-lettered … after 3/3 retries` |

- Повтор іде лише у власну чергу. Через `swaps-events` подію вдруге отримав би й інший сервіс, який її вже обробив.
- Значення `x-retry-count` може прийти як `int`, `long` або `byte[]`, і всі варіанти обробляються.
- Усі логи структуровані: `{EventId}`, `{EventType}`, `{SwapId}`, `{CorrelationId}`, `{Queue}`, `{Attempt}`; винятки — з повним stack trace. На час обробки `CorrelationId`, `EventId`, `EventType`, `Queue`, `Attempt` додаються в `LogContext`.

### Ідемпотентність і порядок подій

**Sessions (SQL Server, EF Core):**
- таблиця `ProcessedEvents (EventId PK, ProcessedAt, EventType)` і колонка `AcceptedSwaps.StatusChangedAt` (міграція `AddSwapEventReplication`);
- `AcceptedSwapReplicator` змінює копію та додає `ProcessedEvent` одним `SaveChanges`, тобто в одній транзакції: подія або застосована й запам'ятована, або ні те, ні інше;
- наявний `EventId` або порушення PK `PK_ProcessedEvents` (SqlException 2627/2601) → `Duplicate event {EventId} skipped`, ack;
- статус застосовується доменним методом `AcceptedSwap.ApplyReplicatedStatus` лише тоді, коли подія новіша за `StatusChangedAt`. Тому запізнілий `SwapAccepted` не відкотить `Completed`.

**Messages (MongoDB):** Mongo в Aspire запущено як один вузол, а не replica set, тому транзакції недоступні й не вмикаються. Натомість кожен крок `ConversationReplicator` ідемпотентний сам по собі:
1. Розмова створюється один раз на обмін: унікальний індекс `ux_conversations_swap`, дублікат ключа означає «вже є». Разом із нею створюється системне повідомлення `SwapAccepted`.
2. Статус змінює `UpdateMany` за `swapId` з фільтром «статус інший **і** `swapStatusUpdatedAt` відсутній або старший за час події». Повтор або запізніла подія нічого не змінюють. Системне повідомлення додається, лише якщо документ справді змінився.
3. Насамкінець `EventId` записується в `processed_events` (унікальний індекс `ux_processed_events_event`, створюється при старті разом з іншими індексами). Дублікат ключа означає, що іншу доставку вже завершено.

Збій між кроками призводить до повтору, і повтор виконує лише ще не зроблені кроки. `swapStatusUpdatedAt` є полем `Conversation`, тому REST-оновлення розмови (наприклад, позначка прочитання) його не стирають.

Кешу цих даних у Sessions і Messages немає. Зведена картка в Aggregator застаріває не більше ніж на свої 30 с.

### Трасування

`Platform.EventBus/TracePropagation.cs`:
- `ActivitySource("Platform.Messaging")`, підключено в ServiceDefaults;
- видавець пише `traceparent`/`tracestate` у заголовки (`Propagators.DefaultTextMapPropagator`);
- споживач відновлює контекст (значення заголовків приходять як `byte[]`) і відкриває Activity типу Consumer `{queue} process` з тегами `messaging.system`, `messaging.destination.name`, `messaging.message.id`, `messaging.operation = process`, `messaging.message.type`.

Результат — один trace:
```
gateway → swaps-api POST /api/swaps/{id}/accept → swaps-events publish
  ├─ sessions.swaps-sync process → SQL
  └─ messages.swaps-sync process → MongoDB
```

### Management UI

У Dashboard → Resources → `messaging` є посилання на ендпоінт management. Логін `guest`. Пароль Aspire згенерував як параметр `messaging-password`: його видно в Dashboard (ресурс `messaging-password`, кнопка показу значення) або командою `dotnet user-secrets list --project Platform.AppHost` (ключ `Parameters:messaging-password`).

### Як відтворити

```bash
U2=7c4d1e22-8a3f-4b6c-b0e9-5f1a2d3c4e02; P=b19e6a33-2c5d-47f8-8d14-9a0b3e6f5c03
curl -X POST http://localhost:5000/api/swaps -H "X-User-Id: $U2" -H "Content-Type: application/json" \
  -d "{\"partnerId\":\"$P\",\"offeredSkillId\":4,\"requestedSkillId\":6}"         # → id
curl -X POST http://localhost:5000/api/swaps/{id}/accept   -H "X-User-Id: $P"  -H "Content-Type: application/json" -d '{}'
curl http://localhost:5000/api/accepted-swaps/{id}/sessions -H "X-User-Id: $U2"   # копія в Sessions
curl http://localhost:5000/api/conversations -H "X-User-Id: $U2"                  # розмова в Messages
curl -X POST http://localhost:5000/api/swaps/{id}/complete -H "X-User-Id: $U2" -H "Content-Type: application/json" -d '{}'
```

1. **Реплікація.** Після accept за секунду з'являються рядок `AcceptedSwaps` і розмова з `swapStatus = Accepted`. Після complete обидві копії мають `Completed`, у розмові є системні повідомлення `SwapAccepted` і `SwapCompleted`.
2. **Формат повідомлення.** У Management UI додайте тимчасову чергу з прив'язкою до `swaps-events` за `swap.*` і прийміть обмін. Get message покаже `message_id`, `type`, `x-correlation-id`, `traceparent`.
3. **Дублікат.**
   - Exchanges → `swaps-events` → Publish message з тими самими властивостями (`message_id`, `type`) і тілом.
   - В обох сервісах у логах `Duplicate event … skipped`, дані не змінюються.
4. **Некоректний JSON.**
   - Exchanges → `(AMQP default)` → Publish message з routing key `sessions.swaps-sync`, властивістю `type = SwapAcceptedEvent` і тілом `{ not json`.
   - Повідомлення одразу опиняється в `sessions.swaps-sync.dlq`, і **лише** в ній (`x-death`: `rejected`). У логах Error `Dead-lettered … JsonException`.
5. **Повтори.**
   - Зупиніть `sql` і опублікуйте подію (accept неможливий, бо Swaps теж на `sql`): у Management UI через `swaps-events` з новим `eventId`.
   - Sessions пише `Retry 1/3`, `2/3`, `3/3` (паузи 1, 2, 4 с), потім `Dead-lettered … after 3/3 retries`, і подія в `sessions.swaps-sync.dlq`. Messages обробляє ту саму подію без проблем.
   - Після запуску `sql` нові події знову обробляються.
6. **Недоступний споживач.**
   - Зупиніть `sessions-api`, прийміть і завершіть кілька обмінів. Події накопичуються в `sessions.swaps-sync` (consumers = 0).
   - Після запуску `sessions-api` усі вони обробляються. Черга durable, повідомлення persistent, а в RabbitMQ є data volume, тож вони переживають і перезапуск брокера.
7. **Trace.** Dashboard → Traces, запит `POST /api/swaps/{id}/accept`. Один trace містить publish і два process з SQL- і Mongo-spans.
