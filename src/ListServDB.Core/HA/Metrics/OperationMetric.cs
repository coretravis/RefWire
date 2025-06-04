namespace ListServDB.Core.HA.Metrics;

/// <summary>
/// Represents aggregated metrics for a single operation.
/// </summary>
public class OperationMetric
{
    /// <summary>
    /// Gets the number of measurements.
    /// </summary>
    public long Count { get; private set; }
    /// <summary>
    /// Gets the total elapsed milliseconds.
    /// </summary>
    public long TotalElapsedMilliseconds { get; private set; }

    /// <summary>
    /// Adds a measurement to the metric.
    /// </summary>
    /// <param name="elapsedMilliseconds">The time elapsed</param>
    public void AddMeasurement(long elapsedMilliseconds)
    {
        Count++;
        TotalElapsedMilliseconds += elapsedMilliseconds;
    }
    /// <summary>
    /// Gets the average latency.
    /// </summary>
    public double AverageLatency => Count > 0 ? (double)TotalElapsedMilliseconds / Count : 0;
}
