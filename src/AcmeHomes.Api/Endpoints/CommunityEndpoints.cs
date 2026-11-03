using System.ComponentModel;

using AcmeHomes.Domain;
using AcmeHomes.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Api.Endpoints;

internal static class CommunityEndpoints
{
    public static void MapCommunityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/communities").WithTags("Communities");

        group.MapGet("/", GetCommunities)
            .WithName("GetCommunities")
            .WithSummary("Lists communities, optionally filtered by market.");

        group.MapGet("/{slug}", GetCommunity)
            .WithName("GetCommunity")
            .WithSummary("Gets a community with the floor plans offered there and their community-specific prices.");
    }

    private static async Task<Ok<List<CommunitySummaryResponse>>> GetCommunities(
        AcmeHomesDbContext db,
        [Description("Market slug, for example \"frederick\".")] string? market,
        CancellationToken cancellationToken)
    {
        var query = db.Communities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(market))
        {
            query = query.Where(c => c.Market.Slug == market);
        }

        var communities = await query
            .OrderBy(c => c.Name)
            .Select(c => new CommunitySummaryResponse(
                c.Slug,
                c.Name,
                c.Market.Slug,
                c.City,
                c.StateCode,
                c.Location.Latitude,
                c.Location.Longitude,
                c.Hoa == null ? null : c.Hoa.MonthlyFee,
                c.CommunityFloorPlans.Min(o => (decimal?)o.BasePrice),
                db.Homes.Count(h => h.CommunityId == c.Id && h.Status != HomeStatus.Sold)))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(communities);
    }

    private static async Task<Results<Ok<CommunityDetailResponse>, NotFound>> GetCommunity(
        AcmeHomesDbContext db,
        string slug,
        CancellationToken cancellationToken)
    {
        var community = await db.Communities
            .Where(c => c.Slug == slug)
            .Select(c => new CommunityDetailResponse(
                c.Slug,
                c.Name,
                new MarketReference(c.Market.Slug, c.Market.Name),
                c.City,
                c.StateCode,
                c.Location.Latitude,
                c.Location.Longitude,
                c.SchoolDistrict,
                c.Hoa == null ? null : new HoaResponse(c.Hoa.MonthlyFee, c.Hoa.Description),
                c.Amenities,
                c.Description,
                c.CommunityFloorPlans
                    .OrderBy(o => o.BasePrice)
                    .Select(o => new OfferedFloorPlanResponse(
                        o.FloorPlan.Slug,
                        o.FloorPlan.Name,
                        o.FloorPlan.Bedrooms,
                        o.FloorPlan.Bathrooms,
                        o.FloorPlan.SquareFeet,
                        o.FloorPlan.Stories,
                        o.FloorPlan.PrimarySuiteLocation,
                        o.BasePrice))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return community is null ? TypedResults.NotFound() : TypedResults.Ok(community);
    }
}

internal sealed record CommunitySummaryResponse(
    string Slug,
    string Name,
    string MarketSlug,
    string City,
    string StateCode,
    double Latitude,
    double Longitude,
    decimal? HoaMonthlyFee,
    decimal? FromPrice,
    int ActiveHomeCount);

internal sealed record CommunityDetailResponse(
    string Slug,
    string Name,
    MarketReference Market,
    string City,
    string StateCode,
    double Latitude,
    double Longitude,
    string SchoolDistrict,
    HoaResponse? Hoa,
    IReadOnlyList<string> Amenities,
    string? Description,
    List<OfferedFloorPlanResponse> FloorPlans);

internal sealed record MarketReference(string Slug, string Name);

internal sealed record HoaResponse(decimal MonthlyFee, string? Description);

internal sealed record OfferedFloorPlanResponse(
    string Slug,
    string Name,
    int Bedrooms,
    decimal Bathrooms,
    int SquareFeet,
    int Stories,
    PrimarySuiteLocation PrimarySuiteLocation,
    decimal BasePrice);
