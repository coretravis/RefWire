using ListServDB.Core.API;
using ListServDB.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ListServDB.WebApi.Endpoints;

/// <summary>
/// Provides extension methods for mapping dataset-related endpoints.
/// </summary>
public static class ItemsEndpoints
{
    /// <summary>
    /// Maps all items-related endpoints to the specified route builder.
    /// </summary>
    /// <param name="routes">The route builder to add the endpoints to.</param>
    /// <returns>The updated route builder.</returns>
    public static IEndpointRouteBuilder MapItemsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/datasets");
        var adminGroup = routes.MapGroup("admin/datasets");

        // GET /datasets/{id}/items - Lists non-archived items in a dataset with pagination.
        group.MapGet("/{id}/items/{skip}/{take}", async (string id, int skip, int take, [FromQuery] string? includeFields, [FromQuery] string? links, [FromServices] IListServApi api) =>
        {
            try
            {
                var items = await api.ListItemsAsync(id, skip, take, MapIncludedFields(includeFields), MapIncludedFields(links));
                return Results.Ok(items);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<IEnumerable<DatasetItem>>(StatusCodes.Status200OK, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("ListDatasetItems")
        .WithDescription("Retrieves non-archived items for the specified dataset with pagination support.");

        // GET /datasets/{id}/items/{itemId} - Retrieves an item by its ID from a dataset.
        group.MapGet("/{id}/items/{itemId}", async (string id, string itemId, [FromServices] IListServApi api) =>
        {
            try
            {
                var item = await api.GetItemByIdAsync(id, itemId);
                return item != null ? Results.Ok(item) : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<DatasetItem>(StatusCodes.Status200OK, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetDatasetItemById")
        .WithDescription("Retrieves an item by its ID from the specified dataset.");

        // POST /datasets/{id}/items/search-by-ids - Searches non-archived items in a dataset by a list of IDs.
        group.MapPost("/{id}/items/search-by-ids", async (string id, [FromBody] IEnumerable<string> itemIds, [FromQuery] string? includeFields, [FromQuery] string? links, [FromServices] IListServApi api) =>
        {
            try
            {
                var items = await api.SearchItemsByIdsAsync(id, itemIds, MapIncludedFields(includeFields), MapIncludedFields(links));
                return Results.Ok(items);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
         .Produces<IEnumerable<DatasetItem>>(StatusCodes.Status200OK, "application/json")
         .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
         .WithName("ListDatasetItemsByIds")
         .WithDescription("Retrieves non-archived items by Id's for the specified dataset with pagination support.");

        // GET /datasets/{id}/items/search - Searches non-archived items in a dataset by NameField.
        group.MapGet("/{id}/items/{skip}/{take}/search", async (string id, int skip, int take, [FromQuery] string? includeFields, [FromQuery] string? links, [FromQuery] string searchTerm, [FromServices] IListServApi api) =>
        {
            try
            {
                var items = await api.SearchItemsAsync(id, searchTerm, skip, take, MapIncludedFields(includeFields), MapIncludedFields(links));
                return Results.Ok(items);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<IEnumerable<DatasetItem>>(StatusCodes.Status200OK, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("SearchDatasetItems")
        .WithDescription("Searches for items in the specified dataset by the NameField using the given search term.");

        // POST /datasets/{id}/items - Adds a new item to a dataset.
        adminGroup.MapPost("/{id}/items", async (string id, [FromBody] DatasetItemCreateRequest request, [FromServices] IListServApi api) =>
        {
            try
            {
                var item = new DatasetItem
                {
                    Id = request.Id,
                    Name = request.Name,
                    Data = request.Data,
                    IsArchived = false
                };
                await api.AddItemAsync(id, item);
                return Results.Created($"/datasets/{id}/items/{request.Id}", item);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<DatasetItem>(StatusCodes.Status201Created, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("AddDatasetItem")
        .WithDescription("Adds a new item to the dataset specified by the ID.");

        // POST /datasets/{id}/items/bulk - Adds multiple new items to a dataset.
        adminGroup.MapPost("/{id}/items/bulk", async (string id,
                                                      [FromBody] DatasetItemBulkCreateRequest request,
                                                      [FromServices] IListServApi api) =>
        {
            try
            {
                var items = request.Items.Select(item => new DatasetItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Data = item.Data,
                    IsArchived = false
                }).ToList();

                await api.AddItemsAsync(id, items);
                return Results.Created($"/datasets/{id}/items/bulk", items);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<DatasetItem>(StatusCodes.Status201Created, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("AddDatasetItems")
        .WithDescription("Adds multiple new items to the dataset specified by the ID.");

        // PUT /datasets/{id}/items/{itemId} - Updates an existing item in a dataset.
        adminGroup.MapPut("/{id}/items/{itemId}", async (string id,
                                                         string itemId,
                                                         [FromBody] DatasetItemUpdateRequest request,
                                                         [FromServices] IListServApi api) =>
        {
            try
            {
                var updatedItem = new DatasetItem
                {
                    Id = itemId,
                    Name = request.Name,
                    Data = request.Data
                };
                await api.UpdateItemAsync(id, updatedItem);
                return Results.Ok(updatedItem);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<DatasetItem>(StatusCodes.Status200OK, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("UpdateDatasetItem")
        .WithDescription("Updates the item identified by itemId in the specified dataset.");

        // DELETE /datasets/{id}/items/{itemId} - Archives (soft-deletes) an item in a dataset.
        adminGroup.MapDelete("/{id}/items/{itemId}", async (string id,
                                                            string itemId,
                                                            [FromBody] DatasetItemArchiveRequest request,
                                                            [FromServices] IListServApi api) =>
        {
            try
            {
                await api.ArchiveItemAsync(id, itemId);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("ArchiveDatasetItem")
        .WithDescription("Archives (soft-deletes) the item identified by itemId from the specified dataset.");

        // GET /datasets/{id}/api/spec - Retrieves the API endpoints spec for the specified dataset.
        adminGroup.MapGet("/{id}/api/spec", async (string id, HttpContext httpContext, [FromServices] IListServApi listServApi) =>
        {
            try
            {
                var dataset = await listServApi.GetDatasetByIdAsync(id);
                // Build the base URL from the current request.
                var request = httpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";

                var spec = new DatasetApiSpec(
                    ListItemsUrl: $"{baseUrl}/datasets/{id}/items/0/10",
                    GetItemByIdUrl: $"{baseUrl}/datasets/{id}/items/{{itemId}}",
                    SearchItemsByIdsUrl: $"{baseUrl}/datasets/{id}/items/search-by-ids",
                    SearchItemsUrl: $"{baseUrl}/datasets/{id}/items/0/10/search?searchTerm={{searchTerm}}",
                    AddItemUrl: $"{baseUrl}/admin/datasets/{id}/items",
                    BulkAddItemsUrl: $"{baseUrl}/admin/datasets/{id}/items/bulk",
                    UpdateItemUrlTemplate: $"{baseUrl}/admin/datasets/{id}/items/{{itemId}}",
                    ArchiveItemUrlTemplate: $"{baseUrl}/admin/datasets/{id}/items/{{itemId}}"
                );

                return Results.Ok(spec);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"Dataset with ID '{id}' not found.");
            }
            catch (Exception)
            {
                return Results.BadRequest("An error occurred while retrieving the dataset.");
            }

        })
        .Produces<DatasetApiSpec>(StatusCodes.Status200OK, "application/json")
        .WithName("GetDatasetApiSpec")
        .WithDescription("Retrieves the API endpoints spec for the specified dataset, with fully qualified URLs that include the server address.");

        return routes;
    }

    /// <summary>
    /// Maps included fields from a comma-separated string to a list of strings.
    /// </summary>
    /// <param name="includeFields"></param>
    /// <returns></returns>
    private static List<string> MapIncludedFields(string? includeFields)
    {
        return string.IsNullOrEmpty(includeFields) ? new List<string>() : includeFields.Trim().Split(",").ToList();
    }
}

/// <summary>
/// Represents a request to create a new item in a dataset.
/// </summary>
/// <param name="Items"> The list of items to create.</param>
public record DatasetItemBulkCreateRequest(List<DatasetItem> Items);

/// <summary>
/// Represents a set of fully qualified API endpoint URLs for a specific dataset.
/// </summary>
public record DatasetApiSpec(
    string ListItemsUrl,
    string GetItemByIdUrl,
    string SearchItemsByIdsUrl,
    string SearchItemsUrl,
    string AddItemUrl,
    string BulkAddItemsUrl,
    string UpdateItemUrlTemplate, // includes a placeholder for the item ID
    string ArchiveItemUrlTemplate // includes a placeholder for the item ID
);