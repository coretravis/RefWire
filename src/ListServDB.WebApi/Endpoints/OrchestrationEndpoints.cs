using ListServDB.Orchestration;
using ListServDB.WebApi.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ListServDB.WebApi.Endpoints;

/// <summary>
/// Provides extension methods for mapping distributed app instance endpoints.
/// </summary>
public static class DistributedAppInstanceManagerEndpoints
{
    private const string DistributedInstanceManagementDisabledMessage = "Distributed instance management is disabled.";

    /// <summary>
    /// Maps all distributed app instance endpoints to the specified route builder.
    /// </summary>
    /// <param name="routes">The route builder to add the endpoints to.</param>
    /// <param name="options">Injected ListServOptions for conditional feature enabling.</param>
    /// <returns>The updated route builder.</returns>
    public static IEndpointRouteBuilder MapDistributedAppInstanceManagerEndpoints(
        this IEndpointRouteBuilder routes,
        IOptions<ListServOptions> options)
    {
        var group = routes.MapGroup("admin/instances");

        // Check if orchestration is enabled
        var useInstances = options.Value.Orchestration.UseOrchestration;
        if (!useInstances)
        {
            group.MapGet("/", () => Results.BadRequest(DistributedInstanceManagementDisabledMessage));
            group.MapDelete("/{id:guid}", (Guid id) => Results.BadRequest(DistributedInstanceManagementDisabledMessage));
            return routes;
        }

        // GET /instances/ - Retrieves all app instances.
        group.MapGet("/", async ([FromServices] IDistributedInstanceManager manager) =>
        {
            var instances = await manager.GetAppInstancesAsync();
            return Results.Ok(instances);
        })
        .Produces<IEnumerable<AppInstance>>(StatusCodes.Status200OK, "application/json")
        .WithName("GetAllAppInstances")
        .WithDescription("Retrieves a list of all distributed app instances.");

        // DELETE /instances/{id} - Removes the app instance identified by the specified ID.
        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] IDistributedInstanceManager manager) =>
        {
            try
            {
                await manager.RemoveAppInstanceAsync(id);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("RemoveAppInstance")
        .WithDescription("Removes the distributed app instance with the specified ID.");

        return routes;
    }
}
