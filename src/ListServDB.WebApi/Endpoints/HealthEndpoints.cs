using ListServDB.Core.API;
using ListServDB.Core.HA;
using ListServDB.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ListServDB.WebApi.Endpoints;

/// <summary>
/// Provides extension methods for mapping health-related endpoints.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps all health-related endpoints to the specified route builder.
    /// </summary>
    /// <param name="routes">The route builder to add the endpoints to.</param>
    /// <returns>The updated route builder.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("admin/health");

        // GET /health - Retrieves a health report for the ListServ system.
        group.MapGet("/", async ([FromServices] IListServApi api, [FromServices] IHealthReportManager healthReportManager) =>
        {
            var state = await api.GetStateAsync();
            var report = healthReportManager.GetHealthReport(state.Datasets ?? new Dictionary<string, Dataset>());
            return TypedResults.Ok(report);
        })
        .Produces<object>(StatusCodes.Status200OK, "application/json")
        .WithName("GetHealthReport")
        .WithDescription("Retrieves a health report of the ListServ system.");
        return routes;
    }
}
