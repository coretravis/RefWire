namespace ListServDB.Core.Caching;

/// <summary>
/// Configuration options for dataset caching
/// </summary>
public class DatasetCacheOptions
{
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(12);
    public int MaxCacheSize { get; set; } = 1000;
}
