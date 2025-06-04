using ListServDB.Security;
using ListServDB.WebApi.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ListServDB.WebApi.Endpoints;

/// <summary>
/// Provides extension methods for mapping API key-related endpoints.
/// </summary>
public static class ApiKeysEndpoints
{
    private const string ApiKeyFeatureDisabledMessage = "API key feature is disabled.";

    /// <summary>
    /// Maps all API key-related endpoints to the specified route builder.
    /// </summary>
    /// <param name="routes">The route builder to add the endpoints to.</param>
    /// <param name="options"></param>
    /// <returns>The updated route builder.</returns>
    public static IEndpointRouteBuilder MapApiKeysEndpoints(this IEndpointRouteBuilder routes,
    IOptions<ListServOptions> options)
    {
        var group = routes.MapGroup("admin/api-keys");

        // Check if API key feature is enabled
        var useApiKeys = options.Value.UseApiKeySecurity;

        // If the feature is disabled, return a bad request for all endpoints
        if (!useApiKeys)
        {
            group.MapPost("/", () => Results.BadRequest(ApiKeyFeatureDisabledMessage));
            group.MapDelete("/{id}", (Guid id) => Results.BadRequest(ApiKeyFeatureDisabledMessage));
            group.MapGet("/", () => Results.BadRequest(ApiKeyFeatureDisabledMessage));
            group.MapGet("/{id}", (Guid id) => Results.BadRequest(ApiKeyFeatureDisabledMessage));
            group.MapPut("/{id}", (Guid id) => Results.BadRequest(ApiKeyFeatureDisabledMessage));
            return routes;
        }

        // POST /admin/api-keys - Creates a new API key.
        group.MapPost("/", async ([FromBody] ApiKeyCreateRequest request, [FromServices] IListServApiKeyService apiKeyService) =>
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiresAt = now.AddDays(30);
                var creationResponse = await apiKeyService.CreateApiKey(request.Name,
                                                                 request.Description,
                                                                 expiresAt,
                                                                 request.Scopes ?? new List<string>());
                return Results.Created($"/api-keys/{creationResponse.Id}", creationResponse);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<ApiKeyCreationResponse>(StatusCodes.Status201Created, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("CreateApiKey")
        .WithDescription("Creates a new API key.");

        // DELETE /admin/api-keys/{id} - Revokes an existing API key.
        group.MapDelete("/{id}", async (Guid id, [FromServices] IListServApiKeyService apiKeyService) =>
        {
            try
            {
                await apiKeyService.DeleteApiKey(id);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("RevokeApiKey")
        .WithDescription("Revokes an existing API key.");

        // GET /admin/api-keys - Lists all API keys.
        group.MapGet("/", async ([FromServices] IListServApiKeyService apiKeyService) =>
        {
            try
            {
                var apiKeys = await apiKeyService.GetApiKeys();
                return Results.Ok(apiKeys);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<IEnumerable<ApiKey>>(StatusCodes.Status200OK, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("ListApiKeys")
        .WithDescription("Lists all API keys.");

        // GET /admin/api-keys/{id} - Retrieves an API key by ID.
        group.MapGet("/{id}", async (Guid id, [FromServices] IListServApiKeyService apiKeyService) =>
        {
            try
            {
                var apiKey = await apiKeyService.GetApiKey(id);
                return Results.Ok(apiKey);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<ApiKey>(StatusCodes.Status200OK, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("GetApiKey")
        .WithDescription("Retrieves an API key by ID.");

        // PUT /admin/api-keys/{id} - Updates an existing API key.
        group.MapPut("/{id}", async (Guid id, [FromBody] ApiKeyCreateRequest request, [FromServices] IListServApiKeyService apiKeyService) =>
        {
            try
            {
                var apiKey = await apiKeyService.UpdateApiKey(id,
                                                              request.Name,
                                                              request.Description,
                                                              request.Scopes ?? new List<string>());
                return Results.Ok(apiKey);

            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<ApiKey>(StatusCodes.Status200OK, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("UpdateApiKey")
        .WithDescription("Updates an existing API key.");

        return routes;
    }
}


/// <summary>
/// Represents a request to create a new API key.
/// </summary>
/// <param name="Name">The name of the API key.</param>
/// <param name="Description">The description of the API key.</param>
/// <param name="Scopes">The scopes associated with the API key.</param>
public record ApiKeyCreateRequest(string Name, string Description, List<string>? Scopes);

/// <summary>
/// Represents a request to revoke an API key.
/// </summary>
/// <param name="Id">The ID of the API key to revoke.</param>
public record ApiKeyRevokeRequest(Guid Id);