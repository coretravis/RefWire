using Microsoft.AspNetCore.Mvc;
using ListServDB.Core.Interfaces;

namespace ListServDB.WebApi.Endpoints;

/// <summary>
/// Provides extension methods for mapping backup-related endpoints.
/// </summary>
public static class BackupEndpoints
{
    /// <summary>
    /// Maps all backup-related endpoints to the specified route builder.
    /// </summary>
    /// <param name="routes">The route builder to add the endpoints to.</param>
    /// <returns>The updated route builder.</returns>
    public static IEndpointRouteBuilder MapBackupEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/backups");

        // GET /backups/{datasetId} - Lists backup files for a dataset.
        group.MapGet("/{datasetId}", async (string datasetId, [FromServices] IDatasetPersistenceManager persistenceManager) =>
        {
            var backups = await persistenceManager.ListBackupsAsync(datasetId);
            return TypedResults.Ok(backups);
        })
        .Produces<IEnumerable<string>>(StatusCodes.Status200OK, "application/json")
        .WithName("ListBackups")
        .WithDescription("Retrieves a list of backup files for the specified dataset.");

        // POST /backups/restore/{datasetId} - Restores a dataset from a backup (admin required).
        group.MapPost("/restore/{datasetId}", async (string datasetId, [FromBody] BackupRestoreRequest request, [FromServices] IDatasetPersistenceManager persistenceManager) =>
        {
            try
            {
                await persistenceManager.RestoreDatasetBackupAsync(datasetId, request.BackupFilePath);
                return Results.Ok($"Restored backup from {request.BackupFilePath}");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .Produces<string>(StatusCodes.Status200OK, "text/plain")
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .WithName("RestoreBackup")
        .WithDescription("Restores a backup for the specified dataset.");

        return routes;
    }
}

/// <summary>
/// Represents a request to restore a backup.
/// </summary>
/// <param name="BackupFilePath">The path of the backup file to restore.</param>
public record BackupRestoreRequest(string BackupFilePath);
