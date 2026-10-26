namespace Matching.Infrastructure.Seeding;

/// <summary>Початкові дані однієї колекції. Реалізація має бути ідемпотентною: повторний запуск не створює дублікатів.</summary>
public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
