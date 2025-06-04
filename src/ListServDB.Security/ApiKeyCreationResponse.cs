namespace ListServDB.Security;

/// <summary>
/// Represents the response for API key creation.
/// </summary>
public class ApiKeyCreationResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyCreationResponse"/> class with specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier of the API key.</param>
    /// <param name="name">The name associated with the API key.</param>
    /// <param name="oneTimeDisplayKey">The one-time display key for the API key.</param>
    public ApiKeyCreationResponse(Guid id, string name, string oneTimeDisplayKey)
    {
        Id = id;
        Name = name;
        OneTimeDisplayKey = oneTimeDisplayKey;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyCreationResponse"/> class.
    /// </summary>
    public ApiKeyCreationResponse() { }

    /// <summary>
    /// Gets or sets the unique identifier of the API key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name associated with the API key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the one-time display key for the API key.
    /// </summary>
    public string OneTimeDisplayKey { get; set; } = string.Empty;
}
