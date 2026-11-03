using AcmeHomes.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Api.Endpoints;

internal static class MarketEndpoints
{
    public static void MapMarketEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/markets").WithTags("Markets");

        group.MapGet("/", GetMarkets)
            .WithName("GetMarkets")
            .WithSummary("Lists the metro areas the builder operates in.");
    }

    private static async Task<Ok<List<MarketResponse>>> GetMarkets(AcmeHomesDbContext db, CancellationToken cancellationToken)
    {
        var markets = await db.Markets
            .OrderBy(m => m.Name)
            .Select(m => new MarketResponse(
                m.Slug,
                m.Name,
                m.StateCode,
                db.Communities.Count(c => c.MarketId == m.Id)))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(markets);
    }
}

internal sealed record MarketResponse(string Slug, string Name, string StateCode, int CommunityCount);
