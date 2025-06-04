using System.ComponentModel.DataAnnotations;

namespace ListServDB.WebApi.Configuration;

/// <summary>
/// Represents the configuration options for ListServ with data annotations for validation.
/// </summary>
public class ListServOptions
{
    /// <summary>
    /// Gets or sets the rate limiting configuration.
    /// </summary>
    public RateLimitingConfig RateLimiting { get; set; } = new RateLimitingConfig();

    /// <summary>
    /// Gets or sets the persistence configuration.
    /// </summary>
    public PersistenceConfig Persistence { get; set; } = new PersistenceConfig();

    /// <summary>
    /// Gets or sets the orchestration configuration.
    /// </summary>
    public OrchestrationConfig Orchestration { get; set; } = new OrchestrationConfig();

    /// <summary>
    /// Gets or sets the CORS configuration.
    /// </summary>
    public CorsConfig Cors { get; set; } = new CorsConfig();

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    [Required(ErrorMessage = "API Key is required")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "API Key must be between 10 and 100 characters")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use API key security.
    /// </summary>
    public bool UseApiKeySecurity { get; set; }

    /// <summary>
    /// Gets or sets the Azure blob connection string.
    /// </summary>
    [Required(ErrorMessage = "Azure Blob Connection String is required")]
    [StringLength(500, ErrorMessage = "Azure Blob Connection String is too long")]
    public string AzureBlobConnectionString { get; set; } = string.Empty;
}
