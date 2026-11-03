using System.Net;
using System.Net.Http.Json;

using AcmeHomes.Api.Endpoints;

namespace AcmeHomes.Api.IntegrationTests;

public class CatalogEndpointTests(AcmeHomesApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Markets_lists_each_market_with_its_community_count()
    {
        var markets = await GetAsync<List<MarketResponse>>("/api/markets");

        Assert.Equal(["baltimore", "frederick"], markets.Select(m => m.Slug));
        Assert.All(markets, m => Assert.Equal(3, m.CommunityCount));
    }

    [Fact]
    public async Task Communities_can_be_filtered_by_market()
    {
        var communities = await GetAsync<List<CommunitySummaryResponse>>("/api/communities?market=frederick");

        Assert.Equal(3, communities.Count);
        Assert.All(communities, c => Assert.Equal("frederick", c.MarketSlug));
    }

    [Fact]
    public async Task Community_detail_includes_offered_plans_with_community_prices()
    {
        var community = await GetAsync<CommunityDetailResponse>("/api/communities/juniper-hollow");

        Assert.Equal("Frederick", community.Market.Name);
        Assert.Contains("Dog park", community.Amenities);
        Assert.Contains(community.FloorPlans, p => p.Slug == "carver" && p.BasePrice == 449_900m);
    }

    [Fact]
    public async Task Community_without_hoa_returns_null_hoa()
    {
        var community = await GetAsync<CommunityDetailResponse>("/api/communities/linden-fields");

        Assert.Null(community.Hoa);
    }

    [Fact]
    public async Task Floor_plan_detail_lists_each_community_price()
    {
        var plan = await GetAsync<FloorPlanDetailResponse>("/api/floor-plans/carver");

        Assert.True(plan.Communities.Count > 1);
        Assert.True(plan.Communities.Select(c => c.BasePrice).Distinct().Count() > 1, "Carver should be priced differently by community.");
        Assert.Contains("Study", plan.Features);
    }

    [Fact]
    public async Task Floor_plans_filtered_by_market_only_use_that_markets_prices()
    {
        var allPlans = await GetAsync<List<FloorPlanSummaryResponse>>("/api/floor-plans");
        var baltimorePlans = await GetAsync<List<FloorPlanSummaryResponse>>("/api/floor-plans?market=baltimore");

        var carverEverywhere = allPlans.Single(p => p.Slug == "carver");
        var carverInBaltimore = baltimorePlans.Single(p => p.Slug == "carver");

        Assert.True(carverInBaltimore.FromPrice > carverEverywhere.FromPrice);
    }

    [Theory]
    [InlineData("/api/communities/no-such-community")]
    [InlineData("/api/floor-plans/no-such-plan")]
    public async Task Unknown_slug_returns_404_problem_details(string url)
    {
        var response = await _client.GetAsync(new Uri(url, UriKind.Relative), _cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApi_document_is_served()
    {
        var response = await _client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), _cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var result = await _client.GetFromJsonAsync<T>(new Uri(url, UriKind.Relative), AcmeHomesApiFactory.JsonOptions, _cancellationToken);
        Assert.NotNull(result);
        return result;
    }
}
