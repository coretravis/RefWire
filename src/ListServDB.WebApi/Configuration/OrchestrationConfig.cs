namespace ListServDB.WebApi.Configuration;

/// <summary>
/// Configuration for orchestration.
/// </summary>
public class OrchestrationConfig
{
    /// <summary>
    /// Name of the blob container.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the control blob for notifications.
    /// </summary>
    public string ControlBlobName { get; set; } = string.Empty;

    /// <summary>
    /// Interval between polling attempts.
    /// </summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Whether to use orchestration.
    /// </summary>
    public bool UseOrchestration { get; set; } = false;

    /// <summary>
    /// The failure threshold for the circuit breaker.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; }

    /// <summary>
    /// The reset timeout for the circuit breaker.
    /// </summary>
    public int CircuitBreakerResetTimeout { get; set; }

    /// <summary>
    /// The maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; }

    /// <summary>
    /// The duration of the lease.
    /// </summary>
    public int LeaseDuration { get; set; }

    /// <summary>
    /// The delays between retry attempts.
    /// </summary>
    public List<int> RetryDelays { get; set; } = new List<int>() { 1, 2, 3, 5, 7, 10 };
}
