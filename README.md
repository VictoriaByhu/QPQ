# QPQ Moderation Service

## Опис проєкту
QPQ Moderation — це мікросервіс (Vertical Slice) для керування скаргами (Reports) та їхніми статусами, побудований на платформі .NET 8/9 за принципами тришарової архітектури (API -> BLL -> DAL). 

Проєкт відповідає всім вимогам Практичного заняття №02:
- **DAL (Data Access Layer):** Реалізовано доступ до бази даних SQL Server через `ADO.NET` та `Dapper`. Використовується патерн `Unit of Work` для гарантії транзакційності операцій.
- **BLL (Business Logic Layer):** Містить бізнес-правила, мапінг DTO через `AutoMapper` та глобальні доменні винятки (`NotFoundException`, `BusinessConflictException`).
- **Web/API:** Тонкі асинхронні контролери з атрибутною маршрутизацією, які повертають правильні HTTP-статуси. Глобальна обробка помилок реалізована через `ProblemDetails`.
- **.NET Aspire:** Усі сервіси (API, БД) піднімаються разом через проєкт-оркестратор `AppHost`.

## Структура рішення
- `QPQ.Moderation.Api` — Web/API, контролери, middleware для перехоплення помилок.
- `QPQ.Moderation.Bll` — Сервіси (`ReportService`), DTO, профілі `AutoMapper`.
- `QPQ.Moderation.Dal` — Репозиторії (ADO.NET / Dapper), інтерфейси (`IGenericRepository`), `Unit of Work`.
- `QPQ.Moderation.Domain` — Моделі, доменні винятки (`BusinessConflictException`).
- `QPQ.Moderation.AppHost` — Оркестратор .NET Aspire для запуску всього середовища.

## Як запустити
1. Переконайтеся, що у вас встановлено **.NET 8 (або 9) SDK** та **Docker Desktop** (для підняття SQL Server через Aspire, якщо база не локальна).
2. Зробіть проєкт **`QPQ.Moderation.AppHost`** стартовим у Visual Studio.
3. Натисніть `F5` або запустіть команду в терміналі з папки `AppHost`:
   ```bash
   dotnet run