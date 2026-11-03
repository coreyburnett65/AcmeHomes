using System.Text.Json.Serialization;

using AcmeHomes.Api.Endpoints;
using AcmeHomes.Infrastructure.Persistence;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry (traces, metrics, logs to the Aspire dashboard), health checks, service discovery, and HTTP resilience.
builder.AddServiceDefaults();

// Aspire's EF Core integration: DbContext pooling, retries on transient errors (including the delay while a serverless
// Azure SQL database resumes from auto-pause), a database health check, and query tracing.
builder.AddSqlServerDbContext<AcmeHomesDbContext>(AcmeHomesDbContext.ConnectionName);

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

app.MapDefaultEndpoints();

app.Run();
