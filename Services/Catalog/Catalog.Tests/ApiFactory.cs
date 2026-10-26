using Catalog.Dal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catalog.Tests;

/// <summary>Піднімає справжній конвеєр API (контролери, валідація, ProblemDetails) на InMemory-базі.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"catalog-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<CatalogDbContext>();
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CatalogDbContext>));
            services.AddDbContext<CatalogDbContext, InMemoryCatalogDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

/// <summary>
/// Для InMemory: rowversion генерує лише SQL Server, тому в початкових даних його немає.
/// Тестовий контекст робить стовпець необов'язковим; у справжній моделі нічого не змінюється.
/// </summary>
public class InMemoryCatalogDbContext(DbContextOptions<InMemoryCatalogDbContext> options) : CatalogDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Catalog.Domain.Models.Category>().Property(c => c.RowVersion).IsRequired(false);
        modelBuilder.Entity<Catalog.Domain.Models.Skill>().Property(c => c.RowVersion).IsRequired(false);
        modelBuilder.Entity<Catalog.Domain.Models.SkillDetail>().Property(c => c.RowVersion).IsRequired(false);
    }
}
