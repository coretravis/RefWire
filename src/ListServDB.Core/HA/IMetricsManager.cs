using ListServDB.Core.HA.Metrics;
using System.Collections.Concurrent;

namespace ListServDB.Core.HA;

/// <summary>
/// Manages the collection and retrieval of operation metrics.
/// </summary>
public interface IMetricsManager
{
    /// <summary>
    /// Gets all the operation metrics.
    /// </summary>
    /// <returns>A concurrent dictionary of operation metrics keyed by operation name.</returns>
    ConcurrentDictionary<string, OperationMetric> GetAllMetrics();

    /// <summary>
    /// Gets the metric for a specified operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>
    /// The metric for the given operation if it exists; otherwise, null.
    /// </returns>
    OperationMetric? GetMetric(string operationName);

    /// <summary>
    /// Records a new measurement for an operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    void Record(string operationName, long elapsedMilliseconds);
}
