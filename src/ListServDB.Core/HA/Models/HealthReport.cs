namespace ListServDB.Core.HA.Models;

/// <summary>
/// Represents the overall health report of the ListServ instance.
/// </summary>
public class HealthReport
{
    /// <summary>
    /// The timestamp (UTC) when the report was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The total number of datasets.
    /// </summary>
    public int TotalDatasets { get; set; }

    /// <summary>
    /// The list of dataset health details.
    /// </summary>
    public List<DatasetHealthReport> DatasetReports { get; set; } = new List<DatasetHealthReport>();

    /// <summary>
    /// Total managed memory in KB.
    /// </summary>
    public long TotalManagedMemoryKB { get; set; }

    /// <summary>
    /// Total processor time used (in milliseconds).
    /// </summary>
    public double TotalProcessorTimeMs { get; set; }

    /// <summary>
    /// Available working set memory in KB.
    /// </summary>
    public long AvailableWorkingSetMemoryKB { get; set; }

    /// <summary>
    /// CPU usage percentage (if available).
    /// </summary>
    public float? CPUUsage { get; set; }

    /// <summary>
    /// Disk space usage percentage (if available).
    /// </summary>
    public float? DiskSpaceUsage { get; set; }

    /// <summary>
    /// The system uptime string (if available).
    /// </summary>
    public string SystemUptime { get; set; } = string.Empty;
}
