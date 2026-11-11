using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Catalog.Api.Data;

/// <summary>
/// Used only by the EF Core tools (dotnet ef migrations ...). It avoids starting the host,
/// which would require the Aspire-injected "catalogdb" connection string and a live database.
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=catalogdb;Username=postgres;Password=design-time-only")
            .Options;

        return new CatalogDbContext(options);
    }
}
