using ECommerce.Catalog.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Catalog.Api.Tests;

/// <summary>Hosts Catalog.Api with its own in-memory database, so tests never share state.</summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"catalog-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Aspire normally injects this setting. The in-memory database never connects to it.
        builder.UseSetting("ConnectionStrings:catalogdb", "Host=catalog.test;Database=catalogdb");

        builder.ConfigureTestServices(services =>
        {
            // AddNpgsqlDbContext registers a pooled context: remove the context, its options,
            // its options configuration and the pool services (all generic over CatalogDbContext).
            var registrations = services
                .Where(d => d.ServiceType == typeof(CatalogDbContext)
                    || d.ServiceType.GenericTypeArguments.Contains(typeof(CatalogDbContext)))
                .ToList();
            foreach (var registration in registrations)
            {
                services.Remove(registration);
            }

            services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
