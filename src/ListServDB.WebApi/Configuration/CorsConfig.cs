namespace ListServDB.WebApi.Configuration;

/// <summary>
/// Configuration for rate limiting.
/// </summary>
public class CorsConfig
{
    private static readonly char[] Separator = new[] { ',' };

    /// <summary>
    /// Gets or sets the allowed origins for CORS.
    /// </summary>
    public string AllowedOrigins { get; set; } = string.Empty;
    /// <summary>
    ///  Gets or sets the allowed methods for CORS.
    /// </summary>
    public string AllowedMethods { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the allowed headers for CORS.
    /// </summary>
    public string AllowedHeaders { get; set; } = string.Empty;
    /// <summary>
    /// Gets the processed list of allowed origins, methods, and headers.
    /// </summary>
    public string[] AllowedOriginsList => ProcessList(AllowedOrigins);
    /// <summary>
    /// Gets the processed list of allowed methods.
    /// </summary>
    public string[] AllowedMethodsList => ProcessList(AllowedMethods);
    /// <summary>
    /// Gets the processed list of allowed headers.
    /// </summary>
    public string[] AllowedHeadersList => ProcessList(AllowedHeaders);

    private static string[] ProcessList(string input)
    {
        return string.IsNullOrEmpty(input)
            ? Array.Empty<string>()
            : input
                .Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
    }
}