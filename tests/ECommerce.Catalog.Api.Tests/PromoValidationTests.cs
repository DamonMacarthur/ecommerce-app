using System.Net;
using System.Net.Http.Json;

namespace ECommerce.Catalog.Api.Tests;

public sealed class PromoValidationTests : IDisposable
{
    private const string PercentError = "The promo percentage must be between 1 and 100.";
    private const string PastDateError = "The promo validity date must not be in the past.";

    private readonly CatalogApiFactory _factory = new();
    private readonly HttpClient _client;

    public PromoValidationTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Theory(DisplayName = "Promo percentage outside 1..100 returns 400 with the percentage message")]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task PercentOutOfRange_Returns400WithMessage(int percent)
    {
        var response = await PostPromoAsync(percent, Tomorrow());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PercentError, await response.Content.ReadFromJsonAsync<string>());
    }

    [Theory(DisplayName = "Promo percentage at the bounds 1 and 100 is accepted")]
    [InlineData(1)]
    [InlineData(100)]
    public async Task PercentAtBound_Returns200(int percent)
    {
        var response = await PostPromoAsync(percent, Tomorrow());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PromoProductResponse>();
        Assert.NotNull(product);
        Assert.Equal(percent, product.PromoPercent);
    }

    [Fact(DisplayName = "Promo ending yesterday (UTC) returns 400 with the past-date message")]
    public async Task ValidUntilYesterday_Returns400WithMessage()
    {
        var response = await PostPromoAsync(10, TodayUtc().AddDays(-1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PastDateError, await response.Content.ReadFromJsonAsync<string>());
    }

    [Fact(DisplayName = "Promo ending today (UTC) is accepted: the end date is inclusive")]
    public async Task ValidUntilToday_Returns200()
    {
        var today = TodayUtc();

        var response = await PostPromoAsync(10, today);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PromoProductResponse>();
        Assert.NotNull(product);
        Assert.Equal(today, product.PromoValidUntil);
    }

    [Fact(DisplayName = "Invalid percentage is reported before a past date")]
    public async Task InvalidPercentAndPastDate_ReportsPercentFirst()
    {
        var response = await PostPromoAsync(0, TodayUtc().AddDays(-1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PercentError, await response.Content.ReadFromJsonAsync<string>());
    }

    private static DateOnly TodayUtc() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static DateOnly Tomorrow() => TodayUtc().AddDays(1);

    private Task<HttpResponseMessage> PostPromoAsync(int percent, DateOnly validUntil) =>
        _client.PostAsJsonAsync("/api/products/1/promo", new { percent, validUntil });

    private sealed record PromoProductResponse(int Id, int? PromoPercent, DateOnly? PromoValidUntil);
}
