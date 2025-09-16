using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace MiniIAM.Endpoints;

public static class HealthEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/health")
            .WithTags("Health")
            .WithOpenApi();

        // Basic health check endpoint
        group.MapGet("/", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
            .WithSummary("Basic health check")
            .WithDescription("Returns a simple healthy status")
            .Produces<object>(StatusCodes.Status200OK);

        // Detailed health check endpoint
        group.MapHealthChecks("/detailed", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });

        // Readiness check endpoint
        group.MapGet("/ready", () => Results.Ok(new { status = "Ready", timestamp = DateTime.UtcNow }))
            .WithSummary("Readiness check")
            .WithDescription("Indicates if the service is ready to accept requests")
            .Produces<object>(StatusCodes.Status200OK);

        // Liveness check endpoint
        group.MapGet("/live", () => Results.Ok(new { status = "Alive", timestamp = DateTime.UtcNow }))
            .WithSummary("Liveness check")
            .WithDescription("Indicates if the service is alive")
            .Produces<object>(StatusCodes.Status200OK);
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                data = entry.Value.Data,
                exception = entry.Value.Exception?.Message
            })
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
