using ListServDB.Core.HA.Models;
using ListServDB.Core.Models;

namespace ListServDB.Core.HA.Health;

/// <summary>
/// Provides a simple health report for the ListServ instance.
/// </summary>
public class HealthReportManager(IHealthMetricsProvider healthMetricsProvider) : IHealthReportManager
{
    private readonly IHealthMetricsProvider _healthMetricsProvider = healthMetricsProvider;

    /// <summary>
    /// Generates a health report summarizing the current state.
    /// </summary>
    public HealthReport GetHealthReport(Dictionary<string, Dataset> datasets)
    {
        var report = new HealthReport
        {
            Timestamp = DateTime.UtcNow,
            TotalDatasets = datasets.Count,
            DatasetReports = new List<DatasetHealthReport>()
        };

        foreach (var ds in datasets.Values)
        {
            int activeItems = ds.Items.Values.Count(item => !item.IsArchived);
            report.DatasetReports.Add(new DatasetHealthReport
            {
                Name = ds.Name,
                Id = ds.Id,
                ActiveItems = activeItems
            });
        }

        report.TotalManagedMemoryKB = _healthMetricsProvider.GetAvailableWorkingSetMemory();
        report.TotalProcessorTimeMs = _healthMetricsProvider.GetTotalProcessorTime();
        report.AvailableWorkingSetMemoryKB = _healthMetricsProvider.GetAvailableWorkingSetMemory();

        report.CPUUsage = _healthMetricsProvider.GetCpuUsage();
        report.DiskSpaceUsage = _healthMetricsProvider.GetDiskSpaceUsage();
        report.SystemUptime = _healthMetricsProvider.GetSystemUptime();
        return report;
    }
}
