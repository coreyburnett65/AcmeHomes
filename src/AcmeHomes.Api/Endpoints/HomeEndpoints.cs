using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using AcmeHomes.Domain;
using AcmeHomes.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Api.Endpoints;

internal static class HomeEndpoints
{
    public static void MapHomeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/homes").WithTags("Homes");

        group.MapGet("/", GetHomes)
            .WithName("GetHomes")
            .WithSummary("Lists homes in inventory with standard filters. Sold homes are excluded unless status=Sold is requested.")
            .ProducesValidationProblem();

        group.MapGet("/{id:int}", GetHome)
            .WithName("GetHome")
            .WithSummary("Gets a home with its floor plan details and features.");
    }

    // The [Range] attributes are enforced by the built-in minimal API validation (AddValidation in Program.cs),
    // which returns a 400 validation problem before this method runs.
    private static async Task<Ok<List<HomeSummaryResponse>>> GetHomes(
        AcmeHomesDbContext db,
        [Description("Market slug, for example \"frederick\".")] string? market,
        [Description("Community slug, for example \"juniper-hollow\".")] string? community,
        [Description("Home status. When omitted, available and under-construction homes are returned.")] HomeStatus? status,
        [Range(1, 10)] int? minBedrooms,
        [Range(1, 10)] decimal? minBathrooms,
        [Range(1, 10_000_000)] decimal? maxPrice,
        CancellationToken cancellationToken)
    {
        var query = db.Homes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(market))
        {
            query = query.Where(h => h.Community.Market.Slug == market);
        }

        if (!string.IsNullOrWhiteSpace(community))
        {
            query = query.Where(h => h.Community.Slug == community);
        }

        query = status is { } requestedStatus
            ? query.Where(h => h.Status == requestedStatus)
            : query.Where(h => h.Status != HomeStatus.Sold);

        if (minBedrooms is { } bedrooms)
        {
            query = query.Where(h => h.FloorPlan.Bedrooms >= bedrooms);
        }

        if (minBathrooms is { } bathrooms)
        {
            query = query.Where(h => h.FloorPlan.Bathrooms >= bathrooms);
        }

        if (maxPrice is { } price)
        {
            query = query.Where(h => h.Price <= price);
        }

        var homes = await query
            .OrderBy(h => h.Price)
            .Select(h => new HomeSummaryResponse(
                h.Id,
                h.Status,
                h.Price,
                h.EstimatedMoveInDate,
                h.LotNumber,
                h.Community.Slug,
                h.Community.Name,
                h.FloorPlan.Slug,
                h.FloorPlan.Name,
                h.FloorPlan.Bedrooms,
                h.FloorPlan.Bathrooms,
                h.FloorPlan.SquareFeet,
                h.FloorPlan.PrimarySuiteLocation))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(homes);
    }

    private static async Task<Results<Ok<HomeDetailResponse>, NotFound>> GetHome(
        AcmeHomesDbContext db,
        int id,
        CancellationToken cancellationToken)
    {
        // Plan and home features are both small, so one query beats the round trips of a split query.
        var home = await db.Homes
            .AsSingleQuery()
            .Where(h => h.Id == id)
            .Select(h => new HomeDetailResponse(
                h.Id,
                h.Status,
                h.Price,
                h.EstimatedMoveInDate,
                h.LotNumber,
                new CommunityReference(h.Community.Slug, h.Community.Name, h.Community.City, h.Community.StateCode),
                new HomeFloorPlanResponse(
                    h.FloorPlan.Slug,
                    h.FloorPlan.Name,
                    h.FloorPlan.Bedrooms,
                    h.FloorPlan.Bathrooms,
                    h.FloorPlan.SquareFeet,
                    h.FloorPlan.Stories,
                    h.FloorPlan.PrimarySuiteLocation),
                // A home has its plan's standard features plus any added to this specific home.
                h.FloorPlan.Features.Select(f => f.Name)
                    .Union(h.Features.Select(f => f.Name))
                    .OrderBy(name => name)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return home is null ? TypedResults.NotFound() : TypedResults.Ok(home);
    }
}

internal sealed record HomeSummaryResponse(
    int Id,
    HomeStatus Status,
    decimal Price,
    DateOnly? EstimatedMoveInDate,
    string LotNumber,
    string CommunitySlug,
    string CommunityName,
    string FloorPlanSlug,
    string FloorPlanName,
    int Bedrooms,
    decimal Bathrooms,
    int SquareFeet,
    PrimarySuiteLocation PrimarySuiteLocation);

internal sealed record HomeDetailResponse(
    int Id,
    HomeStatus Status,
    decimal Price,
    DateOnly? EstimatedMoveInDate,
    string LotNumber,
    CommunityReference Community,
    HomeFloorPlanResponse FloorPlan,
    List<string> Features);

internal sealed record CommunityReference(string Slug, string Name, string City, string StateCode);

internal sealed record HomeFloorPlanResponse(
    string Slug,
    string Name,
    int Bedrooms,
    decimal Bathrooms,
    int SquareFeet,
    int Stories,
    PrimarySuiteLocation PrimarySuiteLocation);
