using System.ComponentModel;

using AcmeHomes.Domain;
using AcmeHomes.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Api.Endpoints;

internal static class FloorPlanEndpoints
{
    public static void MapFloorPlanEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/floor-plans").WithTags("Floor plans");

        group.MapGet("/", GetFloorPlans)
            .WithName("GetFloorPlans")
            .WithSummary("Lists buildable floor plans with their lowest community price, optionally within one market.");

        group.MapGet("/{slug}", GetFloorPlan)
            .WithName("GetFloorPlan")
            .WithSummary("Gets a floor plan with its features and the communities that offer it.");
    }

    private static async Task<Ok<List<FloorPlanSummaryResponse>>> GetFloorPlans(
        AcmeHomesDbContext db,
        [Description("Market slug, for example \"frederick\". Prices and counts only consider communities in this market.")] string? market,
        CancellationToken cancellationToken)
    {
        var filterByMarket = !string.IsNullOrWhiteSpace(market);

        // Each plan's offerings, limited to the requested market, give its "from" price and community count.
        // Plans with no offerings in scope are left out, since they can't be built there.
        var plans = await db.FloorPlans
            .Select(p => new
            {
                Plan = p,
                Offerings = p.CommunityFloorPlans.Where(o => !filterByMarket || o.Community.Market.Slug == market),
            })
            .Where(x => x.Offerings.Any())
            .OrderBy(x => x.Offerings.Min(o => o.BasePrice))
            .Select(x => new FloorPlanSummaryResponse(
                x.Plan.Slug,
                x.Plan.Name,
                x.Plan.Bedrooms,
                x.Plan.Bathrooms,
                x.Plan.SquareFeet,
                x.Plan.Stories,
                x.Plan.PrimarySuiteLocation,
                x.Offerings.Min(o => o.BasePrice),
                x.Offerings.Count()))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(plans);
    }

    private static async Task<Results<Ok<FloorPlanDetailResponse>, NotFound>> GetFloorPlan(
        AcmeHomesDbContext db,
        string slug,
        CancellationToken cancellationToken)
    {
        // Features and communities are both small (a handful each), so one query beats the round trips of a split query.
        var plan = await db.FloorPlans
            .AsSingleQuery()
            .Where(p => p.Slug == slug)
            .Select(p => new FloorPlanDetailResponse(
                p.Slug,
                p.Name,
                p.Bedrooms,
                p.Bathrooms,
                p.SquareFeet,
                p.Stories,
                p.PrimarySuiteLocation,
                p.Description,
                p.Features.OrderBy(f => f.Name).Select(f => f.Name).ToList(),
                p.CommunityFloorPlans
                    .OrderBy(o => o.BasePrice)
                    .Select(o => new PlanOfferingResponse(o.Community.Slug, o.Community.Name, o.Community.Market.Name, o.BasePrice))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return plan is null ? TypedResults.NotFound() : TypedResults.Ok(plan);
    }
}

internal sealed record FloorPlanSummaryResponse(
    string Slug,
    string Name,
    int Bedrooms,
    decimal Bathrooms,
    int SquareFeet,
    int Stories,
    PrimarySuiteLocation PrimarySuiteLocation,
    decimal FromPrice,
    int CommunityCount);

internal sealed record FloorPlanDetailResponse(
    string Slug,
    string Name,
    int Bedrooms,
    decimal Bathrooms,
    int SquareFeet,
    int Stories,
    PrimarySuiteLocation PrimarySuiteLocation,
    string? Description,
    List<string> Features,
    List<PlanOfferingResponse> Communities);

internal sealed record PlanOfferingResponse(string CommunitySlug, string CommunityName, string MarketName, decimal BasePrice);
