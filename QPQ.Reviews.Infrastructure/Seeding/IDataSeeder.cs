using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Infrastructure.Seeding;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}