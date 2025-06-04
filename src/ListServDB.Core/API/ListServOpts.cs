namespace ListServDB.Core.API;

/// <summary>
/// Configuration options for ListServApi
/// </summary>
public class ListServOpts
{
    public int MaxSearchResults { get; set; } = 1000;
    public int MaxLinkedDatasets { get; set; } = 10;
}
