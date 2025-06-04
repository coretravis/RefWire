namespace ListServDB.WebApi.Configuration;

/// <summary>
/// Configuration for rate limiting.
/// </summary>
public class RateLimitingConfig
{
    /// <summary>
    /// Indicates if rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Maximum number of permits allowed.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Time window for rate limiting.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Queue processing order.
    /// </summary>
    public int QueueProcessingOrder { get; set; } = 0;

    /// <summary>
    /// Maximum queue limit.
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// HTTP status code for rate limit rejection.
    /// </summary>
    public int RejectionStatusCode { get; set; } = 429;
}
