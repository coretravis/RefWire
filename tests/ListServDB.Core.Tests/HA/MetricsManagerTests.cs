using ListServDB.Core.HA.Metrics;

namespace ListServDB.Core.Tests.HA;

public class MetricsManagerTests
{
    [Fact]
    public void Record_CreatesNewMetricIfNotExists()
    {
        // Arrange
        var manager = new MetricsManager();

        // Act
        manager.Record("TestOperation", 100);

        // Assert
        var metric = manager.GetMetric("TestOperation");
        Assert.NotNull(metric);
        Assert.Equal(1, metric.Count);
        Assert.Equal(100, metric.TotalElapsedMilliseconds);
    }

    [Fact]
    public void Record_UpdatesExistingMetric()
    {
        // Arrange
        var manager = new MetricsManager();
        manager.Record("TestOperation", 100);

        // Act
        manager.Record("TestOperation", 200);

        // Assert
        var metric = manager.GetMetric("TestOperation");
        Assert.NotNull(metric);
        Assert.Equal(2, metric.Count);
        Assert.Equal(300, metric.TotalElapsedMilliseconds);
    }

    [Fact]
    public void GetMetric_ReturnsNullForNonexistentOperation()
    {
        // Arrange
        var manager = new MetricsManager();

        // Act
        var metric = manager.GetMetric("NonexistentOperation");

        // Assert
        Assert.Null(metric);
    }

    [Fact]
    public void GetAllMetrics_ReturnsAllRecordedMetrics()
    {
        // Arrange
        var manager = new MetricsManager();
        manager.Record("Operation1", 100);
        manager.Record("Operation2", 200);

        // Act
        var allMetrics = manager.GetAllMetrics();

        // Assert
        Assert.Equal(2, allMetrics.Count);
        Assert.True(allMetrics.ContainsKey("Operation1"));
        Assert.True(allMetrics.ContainsKey("Operation2"));
    }

    [Fact]
    public void MultithreadedAccess_HandlesConcurrentDifferentOperations()
    {
        // Arrange
        var manager = new MetricsManager();
        int operationCount = 100;

        // Act
        Parallel.For(0, operationCount, i =>
        {
            manager.Record($"Operation{i}", i);
        });

        // Assert
        var allMetrics = manager.GetAllMetrics();
        Assert.Equal(operationCount, allMetrics.Count);

        // Check each operation has been recorded properly
        for (int i = 0; i < operationCount; i++)
        {
            var metric = manager.GetMetric($"Operation{i}");
            Assert.NotNull(metric);
            Assert.Equal(1, metric.Count);
            Assert.Equal(i, metric.TotalElapsedMilliseconds);
        }
    }
}