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
| Прийнятий обмін | Swaps | Sessions (`AcceptedSwaps`) | `SwapId`, `InitiatorId`, `PartnerId`, `Status` | події `SwapAccepted` (створює копію) і `SwapCompleted` (оновлює статус) |
| Прийнятий обмін | Swaps | Messages (`conversations`) | `swapId`, `swapStatus`, `participants` (`userId`, `role`) | події `SwapAccepted` (створює розмову) і `SwapCompleted` (оновлює `swapStatus`) |
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

Потрібно: .NET 10 SDK та SQL Server LocalDB з базою `QPQ_Swaps`, розгорнутою за кроками розділу «Проєкт №1 (Swaps)». Docker для цього варіанта не потрібен, бо використовується існуюча база.

1. Розгорніть базу p1 (`schema.sql`, `procedures.sql`, `seed.sql`).
2. Запустіть AppHost однією командою:

```
dotnet run --project Platform.AppHost
```

3. Відкрийте Aspire Dashboard за посиланням із консолі: ресурс `swaps-api` має статус Running.
4. Візьміть URL API з Dashboard і відкрийте `<URL>/swagger`.

Рядок підключення `SwapsDb` лежить в `Platform.AppHost/appsettings.json`, Aspire передає його в API через `WithReference`. Без Aspire сервіс запускається окремо (`dotnet run --project Services/Swaps/Swaps.Api`, Swagger на `http://localhost:5180/swagger`) і бере рядок з `Services/Swaps/Swaps.Api/appsettings.json`. Рядок можна перевизначити змінною оточення:

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

