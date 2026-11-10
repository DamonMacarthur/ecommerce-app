using System.Net;
using System.Net.Http.Json;
using ECommerce.Ordering.Api.Data;
using ECommerce.Ordering.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Ordering.Api.Tests;

/// <summary>Hosts Ordering.Api with its own in-memory database and a fake Catalog.</summary>
public sealed class OrderingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ordering-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Aspire normally injects this setting. The fake handler answers instead of the network.
        builder.UseSetting("services:catalog:http:0", "http://catalog.test");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrderingDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<OrderingDbContext>>();
            services.AddDbContext<OrderingDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.AddHttpClient<CatalogServiceClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new FakeCatalogHandler());
        });
    }
}

/// <summary>Answers the Catalog calls Ordering makes. Unknown products get a 404, like the real service.</summary>
public sealed class FakeCatalogHandler : HttpMessageHandler
{
    private const string ProductsPath = "/api/products/";

    // CurrentPrice must be set: Ordering bills it, and a missing value would deserialize as 0.
    private static readonly Dictionary<int, CatalogProduct> Products = new()
    {
        [1] = new CatalogProduct(1, "Mechanical Keyboard", "Hot-swappable RGB keyboard", 119.99m, 50, 119.99m),
        [2] = new CatalogProduct(2, "Wireless Mouse", "Ergonomic 8k DPI mouse", 49.50m, 120, 49.50m),
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (request.Method == HttpMethod.Get
            && path.StartsWith(ProductsPath, StringComparison.Ordinal)
            && int.TryParse(path[ProductsPath.Length..], out var id)
            && Products.TryGetValue(id, out var product))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(product) });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
