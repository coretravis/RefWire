namespace ListServDb.Orchestration.Azure.Blob;

/// <summary>
/// Configuration settings for the Distributed App Instance Manager.
/// </summary>
public class DistributedInstanceManagerConfig
{
    /// <summary>
    /// Gets or sets the threshold number of failures before the circuit breaker trips.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration after which the circuit breaker resets.
    /// </summary>
    public TimeSpan CircuitBreakerResetTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the polling interval.
    /// </summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets an array containing delay times between retries.
    /// </summary>
    public TimeSpan[] RetryDelays { get; set; } = new[]
    {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(5)
        };

    /// <summary>
    /// Gets or sets the lease duration.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(20);
}
