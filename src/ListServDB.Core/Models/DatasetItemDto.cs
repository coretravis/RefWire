namespace ListServDB.Core.Models;

/// <summary>
/// DTO record for dataset items
/// </summary>
public record DatasetItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsArchived { get; init; }
    public Dictionary<string, object> Data { get; init; } = new();
}
