using System.Text.Json.Serialization;

using AcmeHomes.Api.Endpoints;
using AcmeHomes.Infrastructure.Persistence;
using AcmeHomes.Infrastructure.Persistence.Seeding;

using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// The options callback runs when the first context is created, not here, so configuration supplied later
// (Aspire, Azure app settings, or integration test overrides) is already in place when it's read.
builder.Services.AddDbContextPool<AcmeHomesDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AcmeHomes")
        ?? throw new InvalidOperationException("Connection string 'AcmeHomes' is not configured.");

    // Retries cover transient Azure SQL errors, including the delay while a serverless database resumes from auto-pause.
    // Single-query loading is set explicitly: detail endpoints project a couple of small collections (a handful of
    // features and communities), so one round trip beats the extra queries that split loading would add.
    options.UseSqlServer(connectionString, sql => sql
        .EnableRetryOnFailure()
        .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));
    options.UseAcmeHomesSeedData();
});

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Unhandled exceptions and empty error responses (such as 404) become RFC 9457 problem details.
// Request binding failures (for example ?status=Demolished) keep their 400 status instead of becoming a 500.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

// API docs are public on purpose: this is a read-only demo API, and reviewers can explore the deployed version.
app.MapOpenApi();
app.MapScalarApiReference();

// No UseHttpsRedirection: in Azure Container Apps, TLS terminates at the ingress and traffic reaches the app over HTTP.
var api = app.MapGroup("/api");
api.MapMarketEndpoints();
api.MapCommunityEndpoints();
api.MapFloorPlanEndpoints();
api.MapHomeEndpoints();

app.Run();
