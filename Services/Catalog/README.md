# QPQ.Catalog (Лаб. 03, EF Core)

Піддомен **Skills**, API **Catalog**: довідник навичок, категорій, рівнів володіння і тегів. Сервіс володіє `SkillId`; решта сервісів (UserSkills, Matching, Swaps) зберігають лише ідентифікатор і локальну копію назви, яку оновлює подія `SkillChanged`.

Структура рішення така сама, як у Лаб. 02, але доступ до даних будується на EF Core (Code First, Fluent API, міграції, LINQ to Entities).

| Проєкт | Призначення |
|---|---|
| `Catalog.Domain` | сутності з методами-інваріантами (без публічних setter-ів), винятки, специфікації (Ardalis) |
| `Catalog.Dal` | `CatalogDbContext`, `IEntityTypeConfiguration<T>`, міграції, репозиторії, Unit of Work, сідінг |
| `Catalog.Bll` | сервіси, DTO, профіль AutoMapper, валідатори FluentValidation |
| `Catalog.Api` | контролери, Swagger, ProblemDetails, Serilog, запуск міграцій і сідінгу |
| `Catalog.Tests` | модульні й інтеграційні тести (xUnit, NSubstitute, InMemory) |

## Запуск

Потрібні .NET SDK 10 і SQL Server (LocalDB або віддалений). Окрема база **`QPQ_CatalogEf`**: її створюють міграції, тому вона не конфліктує зі скриптами `db/p2/catalog.schema.sql` з Лаб. 01 (база `QPQ_Catalog`).

```bash
# Рядок підключення для Development (User Secrets, пароль не потрапляє в git)
dotnet user-secrets set "ConnectionStrings:CatalogDb" \
  "Server=<host>,<port>;Database=QPQ_CatalogEf;User Id=<login>;Password=<password>;Encrypt=True;TrustServerCertificate=True;" \
  --project Services/Catalog/Catalog.Api

dotnet run --project Services/Catalog/Catalog.Api       # Swagger: http://localhost:5242/swagger
```

За замовчуванням (`appsettings.json`) рядок вказує на LocalDB, тому без секретів сервіс теж запускається на Windows. Під Aspire запуск такий самий, як для UserSkills: `dotnet run --project Platform.AppHost`; рядок `CatalogDb` береться з `Platform.AppHost/appsettings.json` або User Secrets AppHost.

Під час старту сервіс сам виконує `MigrateAsync()` і ідемпотентний сідінг. Це вимикається налаштуваннями `Database:Migrate` і `Database:Seed`; у `Production` сідінг не виконується ніколи.

## Docker

```bash
cd Services/Catalog
docker build -f Catalog.Api/Dockerfile -t qpq-catalog-api .
docker run -p 8080:8080 -e ConnectionStrings__CatalogDb="Server=<host>,<port>;Database=QPQ_CatalogEf;User Id=<login>;Password=<password>;Encrypt=True;TrustServerCertificate=True;" qpq-catalog-api
```

У контейнері `ASPNETCORE_ENVIRONMENT=Production`: міграції застосовуються, а сідінг не виконується (див. нижче).

## Міграції

```bash
dotnet tool install --global dotnet-ef

dotnet ef migrations add InitialCreate --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api
dotnet ef database update              --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api --environment Development
dotnet ef migrations script --idempotent --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api -o db/p2/catalog.ef-migrations.sql
```

Папка міграцій: `Services/Catalog/Catalog.Dal/Migrations/`. Згенерований SQL-скрипт: [`db/p2/catalog.ef-migrations.sql`](../../db/p2/catalog.ef-migrations.sql) (запускати `sqlcmd` треба в уже створеній базі `QPQ_CatalogEf`).

## Модель

| Сутність | Зв'язок | Примітки |
|---|---|---|
| `Category` | 1:N до `Skill` | унікальні `Name`, `Slug` серед активних (фільтровані індекси), м'яке видалення, `RowVersion` |
| `Skill` | головна сутність | `CHECK` на формат `Slug`, унікальна `Name`, індекс `IX_Skills_Category` з `INCLUDE` |
| `SkillDetail` | 1:1 зі `Skill` | PK = FK (`SkillId`) |
| `SkillLevel` | 1:N до `Skill` | рівні 1..5 (`CHECK`), `UNIQUE (SkillId, Level)` |
| `Tag` | M:N зі `Skill` | унікальна `Name` |
| `SkillTag` | проміжна сутність M:N | складений ключ `(SkillId, TagId)`, власне поле `AddedAt` |

Бізнес-правила виконують методи сутностей (`Skill.AddTag`, `SetDetail`, `AddDefaultLevels`, `RenameLevel`, `SoftDelete`), конструктори перевіряють інваріанти. Порушення інваріанта дає `DomainException` (HTTP 409).

## Репозиторії та Unit of Work

| Спосіб завантаження | Метод | SQL-запитів |
|---|---|---|
| Eager Loading | `SkillRepository.GetWithDetailsAsync` (Include Category, Detail, Levels, SkillTags → Tag) | 1 |
| Explicit Loading | `CategoryRepository.GetWithSkillsAsync` (`Entry(...).Collection(...).Query().LoadAsync()`) | 2 |
| LINQ через проміжну сутність M:N | `SkillTagRepository.GetRecentlyTaggedAsync` (фільтр за `Tag.Name` і `AddedAt`) | 1 |

`GenericRepository<T>` має `AddAsync`, `GetByIdAsync`, `GetAllAsync`, `Update`, `Delete`, `SaveChangesAsync`, а також `ListAsync(spec)` і `CountAsync(spec)` для специфікацій. `UnitOfWork` збирає репозиторії; `SaveChangesAsync()` викликається один раз наприкінці бізнес-операції. Помилки бази (`DbUpdateConcurrencyException`, порушення унікальності або FK) перекладаються у `ConflictException`.

## API

| Метод і шлях | Результат |
|---|---|
| `GET /api/skills?page&pageSize&search&categoryId&tag&sortBy&sortDir` | 200, `PagedResult` (`items`, `totalCount`, `page`, `pageSize`, `totalPages`, `hasNext`, `hasPrevious`) і заголовки `X-Total-Count`, `X-Page`, `X-Page-Size`, `X-Total-Pages` |
| `GET /api/skills/{id}` | 200 / 404 |
| `GET /api/skills/by-tag?tagName&since` | 200, навички за тегом (M:N) |
| `POST /api/skills` | 201 + `Location` / 400 / 409 |
| `PUT /api/skills/{id}` | 200 / 400 / 404 / 409 (з `rowVersion` перевіряється паралелізм) |
| `DELETE /api/skills/{id}` | 204 / 404 (м'яке видалення) |
| `POST` і `DELETE /api/skills/{id}/tags/{tagId}` | 204 / 404 / 409 |
| `PUT /api/skills/{id}/levels/{level}` | 200 / 400 / 404 |
| `GET /api/categories`, `GET /api/categories/{id}` | 200 (друга форма: категорія з навичками, Explicit Loading) |
| `POST`, `PUT`, `DELETE /api/categories` | 201 / 200 / 204; видалення категорії з активними навичками дає 409 |
| `GET`, `POST /api/tags` | 200 / 201 |

`sortBy`: `name` (за замовчуванням), `category`, `createdAt`; `sortDir`: `asc` / `desc`. `pageSize` обмежено 100. Фільтрація, сортування, підрахунок `totalCount` і `OFFSET/FETCH` виконуються в SQL.

Приклади запитів: [`Catalog.Api/Catalog.Api.http`](Catalog.Api/Catalog.Api.http).

## Валідація й помилки

* DataAnnotations на DTO: обов'язковість, довжини, діапазони (автоматично дають 400 `ValidationProblemDetails`).
* FluentValidation (явний виклик у контролері): формат `slug`, абсолютний `http(s)` URL, унікальні `tagIds`, формат `rowVersion`.
* `GlobalExceptionHandler` (`IExceptionHandler` + `AddProblemDetails`): `EntityNotFoundException` → 404, `ValidationException` → 400, `ConflictException` і `DomainException` → 409, інше → 500 (деталі лише в Development). Кожен виняток логується один раз.

## Сідінг

* Статичний (`HasData` у конфігураціях): категорії, навички (`SkillId` 1-6 збігаються з UserSkills, Swaps і Sessions), теги. Фіксовані Id потрібні, бо інші сервіси посилаються на них.
* Програмний (`DbInitializer.SeedAsync` після `MigrateAsync`): описи, п'ять рівнів і теги навичок. Додає лише відсутнє, тому повторний запуск не створює дублів.

## Логування

Serilog (`Serilog.AspNetCore`), sink: console. У кожному записі є `CorrelationId` (заголовок `X-Correlation-Id` або `TraceIdentifier`), `UserId` (claim `sub` або `X-User-Id`, інакше `anonymous`) і `RequestPath`. У `appsettings.Development.json` увімкнено SQL-команди EF Core (`Microsoft.EntityFrameworkCore.Database.Command`), за ними видно, що Eager Loading виконує один запит, а Explicit Loading два.

## Специфікації (Ardalis.Specification)

`SkillsFilterSpec` (Where, Include, OrderBy, Skip/Take), `CategoriesFilterSpec` і `SkillsByCategorySpec` описують вибірки без залежності від EF. Репозиторій лише застосовує їх через `WithSpecification(spec)`; нова специфікація працює без змін у репозиторії (див. `SkillsByCategorySpec`).

## Тести

```bash
dotnet test Services/Catalog/Catalog.Tests
```

Модульні тести доменної моделі, специфікацій (на звичайній колекції), мапінгу й валідації, а також інтеграційні тести повного конвеєра API на InMemory-базі: пагінація з `totalCount`, фільтри, 400/404/409 у форматі ProblemDetails, 201 + `Location`, ідемпотентність сідінгу. InMemory не рахує SQL-запити й не перевіряє унікальні індекси, тому кількість запитів (1 і 2) перевіряється за логом на справжньому SQL Server.
