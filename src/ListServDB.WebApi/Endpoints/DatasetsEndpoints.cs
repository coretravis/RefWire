using ListServDB.Core.API;
using ListServDB.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ListServDB.WebApi.Endpoints;

/// <summary>
/// Provides extension methods for mapping dataset-related endpoints.
/// </summary>
public static class DatasetsEndpoints
{
    /// <summary>
    /// Maps all dataset-related endpoints to the specified route builder.
    /// </summary>
    /// <param name="routes">The route builder to add the endpoints to.</param>
    /// <returns>The updated route builder.</returns>
    public static IEndpointRouteBuilder MapDatasetsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/datasets");
        var adminGroup = routes.MapGroup("admin/datasets");

        // GET /datasets/ - Gets all datasets.
        group.MapGet("/", async ([FromServices] IListServApi api) =>
        {
            var datasets = await api.GetAllDatasetsAsync();
            var result = datasets.Select(ds => new
            {
                ds.Id,
                ds.Name,
                ds.IdField,
                ds.NameField,
                ds.Fields
            });
            return TypedResults.Ok(result);
        })
        .Produces<IEnumerable<string>>(StatusCodes.Status200OK, "application/json")
        .WithName("GetAllDatasets")
        .WithDescription("Retrieves a list of all datasets.");

        // GET /datasets/{id} - Gets the details of a specific dataset by ID.
        group.MapGet("/{id}", async (
            string id,
            [FromQuery] string? includeFields,
            [FromQuery] string? links,
            [FromServices] IListServApi api) =>
        {
            try
            {
                // Parse comma-separated query parameters into arrays
                IEnumerable<string>? includeFieldsArray = null;
                if (!string.IsNullOrWhiteSpace(includeFields))
                {
                    includeFieldsArray = includeFields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(f => f.Trim())
                                                     .Where(f => !string.IsNullOrEmpty(f));
                }

                IEnumerable<string>? linksArray = null;
                if (!string.IsNullOrWhiteSpace(links))
                {
                    linksArray = links.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(l => l.Trim())
                                     .Where(l => !string.IsNullOrEmpty(l));
                }

                var dataset = await api.GetDatasetByIdAsync(id, includeFieldsArray, linksArray);
                return dataset is not null
                    ? Results.Ok(dataset)
                    : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<Dataset>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("GetDatasetById")
        .WithDescription("Retrieves the details of the dataset identified by the specified ID. " +
                        "Optionally filter fields using 'includeFields' (comma-separated) and " +
                        "include linked data using 'links' (comma-separated, format: datasetId-fieldName).");

        // GET /datasets/{id}/items - Gets all items in a specific dataset by ID.
        adminGroup.MapGet("/{id}/meta", async (string id, [FromServices] IListServApi api) =>
        {
            try
            {
                var dataset = await api.GetDatasetMetaAsync(id);
                return dataset is not null
                    ? Results.Ok(dataset)
                    : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<DatasetMeta>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("GetDatasetMeta")
        .WithDescription("Retrieves the metadata of the dataset identified by the specified ID.");

        // GET /datasets/{id}/items - Gets all items in a specific dataset by ID.
        adminGroup.MapGet("/list", async ([FromServices]  IListServApi api) =>
        {
            var datasets = await api.ListDatasetIdsAsync();
            return TypedResults.Ok(datasets);
        })
        .Produces<IEnumerable<string>>(StatusCodes.Status200OK, "application/json")
        .WithName("GetAllDatasetsList")
        .WithDescription("Retrieves a list of all datasets ids.");

        // POST /datasets/{id}/items - Creates a new item in a dataset.
        adminGroup.MapPost("/", async ([FromBody] DatasetCreateRequest request, [FromServices] IListServApi api) =>
        {
            try
            {
                var dataset = await api.CreateDatasetAsync(
                    request.Id,
                    request.Name,
                    request.Description,
                    request.IdField,
                    request.NameField,
                    request.Fields, 
                    request.Items);

                // create a new dataset mapping excluding the items
                var datasetMapping = new Dataset
                {
                    Id = dataset.Id,
                    Name = dataset.Name,
                    Description = dataset.Description,
                    IdField = dataset.IdField,
                    NameField = dataset.NameField,
                    Fields = dataset.Fields
                };

                return Results.Created($"/datasets/{dataset.Id}", datasetMapping);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<Dataset>(StatusCodes.Status201Created, "application/json")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("CreateDataset")
        .WithDescription("Creates a new dataset with the provided information.");

        // POST /datasets/{id}/items - Creates a new item in a dataset.
        adminGroup.MapPut("/{id}", async (string id, [FromBody] DatasetUpdateRequest request, [FromServices] IListServApi api) =>
        {
            try
            {
                var dataset = await api.GetDatasetByIdAsync(id);
                if (dataset == null)
                {
                    return Results.NotFound();
                }

                // Update mutable properties.
                dataset.Name = request.Name;
                dataset.Fields = request.Fields;
                await api.UpdateDatasetAsync(dataset);
                return Results.Ok(dataset);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<Dataset>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status404NotFound)
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("UpdateDataset")
        .WithDescription("Updates the dataset identified by the specified ID.");

        // GET /datasets/state - Gets the overall state snapshot.
        adminGroup.MapGet("/state", async ([FromServices] IListServApi api) =>
        {
            var state = await api.GetStateAsync();
            return Results.Ok(state);
        })
        .Produces<object>(StatusCodes.Status200OK, "application/json")
        .WithName("GetState")
        .WithDescription("Retrieves the overall state snapshot of the ListServ system.");

        // DELETE /admin/datasets/{id} - Deletes a dataset.
        adminGroup.MapDelete("/{id}", async (string id, [FromServices] IListServApi api) =>
        {
            try
            {
                await api.DeleteDataset(id);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("DeleteDataset")
        .WithDescription("Deletes the dataset identified by the specified ID.");

        return routes;
    }
}

/// <summary>
/// Represents a request to create a new dataset.
/// </summary>
/// <param name="Id">The unique identifier for the dataset.</param>
/// <param name="Name">The name of the dataset.</param>
/// <param name="IdField">The field used as the unique identifier for Items in the dataset.</param>
/// <param name="NameField">The field used as the display name for Items in the dataset.</param>
/// <param name="Description"> The description of the dataset.</param>
/// <param name="Fields">The list of fields defining the schema for Items in the dataset.</param>
/// <param name="Items"></param>
public record DatasetCreateRequest(
    string Id,
    string Name,
    string Description,
    string IdField,
    string NameField,    
    List<DatasetField> Fields, Dictionary<string, DatasetItem> Items);

/// <summary>
/// Represents a request to update an existing dataset.
/// </summary>
/// <param name="Name">The new name of the dataset.</param>
/// <param name="Fields">The new list of fields defining the schema for Items in the dataset.</param>
/// <param name="Username">The username for authentication.</param>
/// <param name="Password">The password for authentication.</param>
public record DatasetUpdateRequest(
    string Name,
    List<DatasetField> Fields,
    string Username,
    string Password);

/// <summary>
/// Represents a request to create a new item in a dataset.
/// </summary>
/// <param name="Id">The unique identifier for the item.</param>
/// <param name="Name">The name of the item.</param>
/// <param name="Data">The data for the item.</param>
public record DatasetItemCreateRequest(
    string Id,
    string Name,
    Dictionary<string, object> Data);

/// <summary>
/// Represents a request to update an existing item in a dataset.
/// </summary>
/// <param name="Name">The new name for the item.</param>
/// <param name="Data">The new data for the item.</param>
public record DatasetItemUpdateRequest(
    string Name,
    Dictionary<string, object> Data);

/// <summary>
/// Represents a request to archive (soft-delete) an item in a dataset.
/// </summary>
public record DatasetItemArchiveRequest();
