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

## Resilience-політики (Лабораторна робота №7)

Для клієнтів `Platform.Aggregator` налаштовано окремі політики стійкості залежно від критичності сервісу (після виклику `RemoveAllResilienceHandlers()` для уникнення нашарування):

| Клієнт / Сервіс | Критичність | Retry | Таймаути (Attempt / Total) | Circuit Breaker | Обґрунтування |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`reviews-api` (gRPC)** | Некритичний | HTTP: 1 спроба (0.5 с, exp + jitter)<br>gRPC: до 3 спроб (`Unavailable`, `ResourceExhausted`) | 2 с / 5 с | `FailureRatio: 0.7`<br>`MinimumThroughput: 5`<br>`Sampling: 10 с`<br>`Break: 15 с` | Агрегатор може повернути часткову відповідь без відгуків. Короткі таймаути та мінімум повторів запобігають затримці основного запиту. |
| **`swap-service` (HTTP)** | Критичний | 4 спроби (1 с, exp + jitter, лише ідемпотентні методи) | 4 с / 25 с | `FailureRatio: 0.3`<br>`MinimumThroughput: 5`<br>`Sampling: 30 с`<br>`Break: 15 с` | Критична залежність платформи: більше повторних спроб для поглинання тимчасових збоїв і чутливіший запобіжник для швидкого захисту від каскадних відмов. |
| **Redis (кеш)** | Необов'язкова залежність | Вбудоване перепідключення `ConnectionMultiplexer` | Health check: 3 с | Graceful degradation (`try/catch`) | Кеш не входить до критичного шляху: при недоступності Redis сервіс читає дані з MongoDB, а health check переходить у статус `Degraded`. |