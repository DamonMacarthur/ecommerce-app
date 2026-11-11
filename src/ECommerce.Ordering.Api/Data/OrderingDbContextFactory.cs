using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Ordering.Api.Data;

/// <summary>
/// Used only by the EF Core tools (dotnet ef migrations ...). It avoids starting the host,
/// which would require the Aspire-injected "orderingdb" connection string and a live database.
/// </summary>
public sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=orderingdb;Username=postgres;Password=design-time-only")
            .Options;

        return new OrderingDbContext(options);
    }
}
