using System.Net;
using System.Net.Http.Json;

using AcmeHomes.Api.Endpoints;
using AcmeHomes.Domain;

namespace AcmeHomes.Api.IntegrationTests;

public class HomeEndpointTests(AcmeHomesApiFactory factory)
{
    private static readonly string[] FrederickCommunities = ["juniper-hollow", "stonebrook-meadows", "linden-fields"];

    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sold_homes_are_excluded_by_default()
    {
        var homes = await GetAsync<List<HomeSummaryResponse>>("/api/homes");

        Assert.NotEmpty(homes);
        Assert.DoesNotContain(homes, h => h.Status == HomeStatus.Sold);
    }

    [Fact]
    public async Task Sold_homes_are_returned_when_requested()
    {
        var homes = await GetAsync<List<HomeSummaryResponse>>("/api/homes?status=Sold");

        Assert.All(homes, h => Assert.Equal(HomeStatus.Sold, h.Status));
    }

    [Fact]
    public async Task Filters_combine_and_results_are_sorted_by_price()
    {
        var homes = await GetAsync<List<HomeSummaryResponse>>("/api/homes?market=frederick&minBedrooms=4&maxPrice=600000");

        Assert.NotEmpty(homes);
        Assert.All(homes, h =>
        {
            Assert.True(h.Bedrooms >= 4);
            Assert.True(h.Price <= 600_000m);
            Assert.Contains(h.CommunitySlug, FrederickCommunities);
        });
        Assert.Equal(homes.OrderBy(h => h.Price).Select(h => h.Id), homes.Select(h => h.Id));
    }

    [Fact]
    public async Task Home_detail_combines_plan_and_home_features()
    {
        var summary = (await GetAsync<List<HomeSummaryResponse>>("/api/homes")).First();

        var home = await GetAsync<HomeDetailResponse>($"/api/homes/{summary.Id}");

        Assert.Equal(summary.FloorPlanSlug, home.FloorPlan.Slug);
        Assert.NotEmpty(home.Features);
        Assert.Equal(home.Features.Distinct(), home.Features);
    }

    [Fact]
    public async Task Unknown_home_returns_404()
    {
        var response = await _client.GetAsync(new Uri("/api/homes/999999", UriKind.Relative), _cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("minBedrooms=0")]
    [InlineData("maxPrice=-5")]
    public async Task Out_of_range_filters_return_400_validation_problem(string queryString)
    {
        var response = await _client.GetAsync(new Uri($"/api/homes?{queryString}", UriKind.Relative), _cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unknown_status_value_returns_400()
    {
        var response = await _client.GetAsync(new Uri("/api/homes?status=Demolished", UriKind.Relative), _cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var result = await _client.GetFromJsonAsync<T>(new Uri(url, UriKind.Relative), AcmeHomesApiFactory.JsonOptions, _cancellationToken);
        Assert.NotNull(result);
        return result;
    }
}
