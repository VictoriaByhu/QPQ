# Мікросервісна платформа на .NET Aspire

Цей проєкт реалізує мікросервісну архітектуру з використанням .NET Aspire для оркестрації, YARP для API Gateway, Serilog для структурованого логування та OpenTelemetry для розподіленого трасування.

## 🏗 Структура рішення

- **Platform.AppHost**: Оркестратор системи. Відповідає за запуск усіх мікросервісів, API Gateway, Aggregator та контейнерів баз даних (SQL Server, MongoDB).
- **Platform.ServiceDefaults**: Спільна бібліотека налаштувань. Містить конфігурацію Serilog, OpenTelemetry, Health Checks та наскрізну передачу `CorrelationId`.
- **Platform.ApiGateway**: Єдина точка входу (YARP). Маршрутизує запити за логічними іменами, генерує `CorrelationId` для нових запитів та логує маршрутизацію.
- **Platform.Aggregator**: Сервіс композиції даних. Паралельно (`Task.WhenAll`) опитує мікросервіси та формує єдину відповідь (Composite DTO), обробляючи часткові відмови.
- **QPQ.Reviews.WebApi (та інші)**: Незалежні мікросервіси з власною бізнес-логікою та базами даних.

## 🚀 Інструкція із запуску

### Вимоги
1. Встановлений **.NET 9.0 SDK** (або 8.0).
2. Запущений **Docker Desktop** (необхідний для підняття контейнерів баз даних MS SQL та MongoDB).
3. Visual Studio 2022 (v17.12+) або Rider.

### Запуск через Visual Studio
1. Відкрийте рішення `QPQ.sln`.
2. Встановіть проєкт **`Platform.AppHost`** як стартовий (Set as Startup Project).
3. Натисніть **F5** (або Ctrl+F5).
4. Автоматично відкриється браузер із **.NET Aspire Dashboard**.

### Запуск через .NET CLI
Відкрийте термінал у кореневій папці проєкту та виконайте:
```bash
dotnet run --project Platform.AppHost/Platform.AppHost.csproj