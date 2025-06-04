using ListServDB.Core.HA;
using ListServDB.Core.HA.Health;
using ListServDB.Core.Models;
using Moq;

namespace ListServDB.Core.Tests.HA;

public class HealthReportManagerTests
{
    private readonly Mock<IHealthMetricsProvider> _mockMetricsProvider;
    private readonly HealthReportManager _healthReportManager;

    public HealthReportManagerTests()
    {
        // Setup mock for the metrics provider
        _mockMetricsProvider = new Mock<IHealthMetricsProvider>();
        _healthReportManager = new HealthReportManager(_mockMetricsProvider.Object);

        // Setup default metric values
        _mockMetricsProvider.Setup(m => m.GetAvailableWorkingSetMemory()).Returns(1024);
        _mockMetricsProvider.Setup(m => m.GetTotalProcessorTime()).Returns(500);
        _mockMetricsProvider.Setup(m => m.GetCpuUsage()).Returns(25.5f);
        _mockMetricsProvider.Setup(m => m.GetDiskSpaceUsage()).Returns(75.0f);
        _mockMetricsProvider.Setup(m => m.GetSystemUptime()).Returns(TimeSpan.FromHours(48).ToString());
    }

    [Fact]
    public void GetHealthReport_EmptyDatasets_ReturnsReportWithZeroDatasets()
    {
        // Arrange
        var datasets = new Dictionary<string, Dataset>();

        // Act
        var report = _healthReportManager.GetHealthReport(datasets);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(0, report.TotalDatasets);
        Assert.Empty(report.DatasetReports);
        Assert.Equal(1024, report.AvailableWorkingSetMemoryKB);
        Assert.Equal(500, report.TotalProcessorTimeMs);
        Assert.Equal(25.5f, report.CPUUsage);
        Assert.Equal(75.0f, report.DiskSpaceUsage);
        Assert.Equal(TimeSpan.FromHours(48).ToString(), report.SystemUptime);
    }

    [Fact]
    public void GetHealthReport_WithDatasets_ReturnsCorrectDatasetCount()
    {
        // Arrange
        var datasets = new Dictionary<string, Dataset>
        {
            { "dataset1", new Dataset { Id = "1", Name = "Test Dataset 1", Items = new Dictionary<string, DatasetItem>() } },
            { "dataset2", new Dataset { Id = "2", Name = "Test Dataset 2", Items = new Dictionary<string, DatasetItem>() } }
        };

        // Act
        var report = _healthReportManager.GetHealthReport(datasets);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(2, report.TotalDatasets);
        Assert.Equal(2, report.DatasetReports.Count);
        Assert.Contains(report.DatasetReports, dr => dr.Id == "1" && dr.Name == "Test Dataset 1");
        Assert.Contains(report.DatasetReports, dr => dr.Id == "2" && dr.Name == "Test Dataset 2");
    }

    [Fact]
    public void GetHealthReport_WithItems_CountsActiveItemsCorrectly()
    {
        // Arrange
        var dataset = new Dataset
        {
            Id = "1",
            Name = "Test Dataset",
            Items = new Dictionary<string, DatasetItem>
            {
                { "item1", new DatasetItem { Id = "item1", IsArchived = false } },
                { "item2", new DatasetItem { Id = "item2", IsArchived = true } },
                { "item3", new DatasetItem { Id = "item3", IsArchived = false } }
            }
        };

        var datasets = new Dictionary<string, Dataset>
        {
            { "dataset1", dataset }
        };

        // Act
        var report = _healthReportManager.GetHealthReport(datasets);

        // Assert
        Assert.NotNull(report);
        Assert.Single(report.DatasetReports);
        Assert.Equal(2, report.DatasetReports[0].ActiveItems);
    }

    [Fact]
    public void GetHealthReport_MultipleDatasets_CountsActiveItemsPerDatasetCorrectly()
    {
        // Arrange
        var dataset1 = new Dataset
        {
            Id = "1",
            Name = "Test Dataset 1",
            Items = new Dictionary<string, DatasetItem>
            {
                { "item1", new DatasetItem { Id = "item1", IsArchived = false } },
                { "item2", new DatasetItem { Id = "item2", IsArchived = true } }
            }
        };

        var dataset2 = new Dataset
        {
            Id = "2",
            Name = "Test Dataset 2",
            Items = new Dictionary<string, DatasetItem>
            {
                { "item3", new DatasetItem { Id = "item3", IsArchived = false } },
                { "item4", new DatasetItem { Id = "item4", IsArchived = false } },
                { "item5", new DatasetItem { Id = "item5", IsArchived = true } }
            }
        };

        var datasets = new Dictionary<string, Dataset>
        {
            { "dataset1", dataset1 },
            { "dataset2", dataset2 }
        };

        // Act
        var report = _healthReportManager.GetHealthReport(datasets);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(2, report.TotalDatasets);
        Assert.Equal(2, report.DatasetReports.Count);

        var datasetReport1 = report.DatasetReports.FirstOrDefault(dr => dr.Id == "1");
        var datasetReport2 = report.DatasetReports.FirstOrDefault(dr => dr.Id == "2");

        Assert.NotNull(datasetReport1);
        Assert.NotNull(datasetReport2);
        Assert.Equal(1, datasetReport1.ActiveItems);
        Assert.Equal(2, datasetReport2.ActiveItems);
    }

    [Fact]
    public void GetHealthReport_SystemMetricsAreFetched()
    {
        // Arrange
        var datasets = new Dictionary<string, Dataset>();

        // Act
        var report = _healthReportManager.GetHealthReport(datasets);

        // Assert
        _mockMetricsProvider.Verify(m => m.GetAvailableWorkingSetMemory(), Times.Exactly(2)); 
        _mockMetricsProvider.Verify(m => m.GetTotalProcessorTime(), Times.Once);
        _mockMetricsProvider.Verify(m => m.GetCpuUsage(), Times.Once);
        _mockMetricsProvider.Verify(m => m.GetDiskSpaceUsage(), Times.Once);
        _mockMetricsProvider.Verify(m => m.GetSystemUptime(), Times.Once);
    }

    [Fact]
    public void GetHealthReport_TimestampIsCurrentUtcTime()
    {
        // Arrange
        var datasets = new Dictionary<string, Dataset>();
        var beforeTest = DateTime.UtcNow;

        // Act
        var report = _healthReportManager.GetHealthReport(datasets);
        var afterTest = DateTime.UtcNow;

        // Assert
        Assert.InRange(report.Timestamp, beforeTest, afterTest);
    }
}