using System.Collections.Concurrent;

namespace ListServDB.Core.HA.Metrics;

/// <summary>
/// Collects and aggregates metrics for ListServ operations.
/// </summary>
public class MetricsManager : IMetricsManager
{
    private readonly ConcurrentDictionary<string, OperationMetric> _metrics;

    public MetricsManager()
    {
        _metrics = new ConcurrentDictionary<string, OperationMetric>();
    }
    public void Record(string operationName, long elapsedMilliseconds)
    {
        var metric = _metrics.GetOrAdd(operationName, _ => new OperationMetric());
        metric.AddMeasurement(elapsedMilliseconds);
    }
    public OperationMetric? GetMetric(string operationName)
    {
        _metrics.TryGetValue(operationName, out var metric);
        return metric;
    }
    public ConcurrentDictionary<string, OperationMetric> GetAllMetrics() => _metrics;
}
