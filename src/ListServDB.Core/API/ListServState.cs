using ListServDB.Core.Models;

namespace ListServDB.Core.API;

/// <summary>
/// Represents the state of the ListServ API.
/// </summary>
public class ListServState
{
    /// <summary>
    /// Gets or sets the datasets available in the API.
    /// </summary>
    public Dictionary<string, Dataset>? Datasets { get; set; }
}
