using ListServDB.Core.HA.Metrics;

namespace ListServDB.Core.Tests.HA;

public class OperationMetricTests
{
    [Fact]
    public void AddMeasurement_IncrementsCount()
    {
        // Arrange
        var metric = new OperationMetric();

        // Act
        metric.AddMeasurement(100);

        // Assert
        Assert.Equal(1, metric.Count);
    }

    [Fact]
    public void AddMeasurement_AccumulatesElapsedTime()
    {
        // Arrange
        var metric = new OperationMetric();

        // Act
        metric.AddMeasurement(100);
        metric.AddMeasurement(200);

        // Assert
        Assert.Equal(300, metric.TotalElapsedMilliseconds);
    }

    [Fact]
    public void AverageLatency_CalculatesCorrectAverage()
    {
        // Arrange
        var metric = new OperationMetric();

        // Act
        metric.AddMeasurement(100);
        metric.AddMeasurement(200);

        // Assert
        Assert.Equal(150.0, metric.AverageLatency);
    }

    [Fact]
    public void AverageLatency_ReturnsZeroForNoMeasurements()
    {
        // Arrange
        var metric = new OperationMetric();

        // Act & Assert
        Assert.Equal(0, metric.AverageLatency);
    }
}
