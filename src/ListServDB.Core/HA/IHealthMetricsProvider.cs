namespace ListServDB.Core.HA;

/// <summary>
/// Provides methods to retrieve various health metrics of the system.
/// </summary>
public interface IHealthMetricsProvider
{
    /// <summary>
    /// Gets the current CPU usage as a percentage.
    /// </summary>
    /// <returns>A nullable float representing the CPU usage percentage, or null if unavailable.</returns>
    float? GetCpuUsage();

    /// <summary>
    /// Gets the current disk space usage as a percentage.
    /// </summary>
    /// <returns>A nullable float representing the disk space usage percentage, or null if unavailable.</returns>
    float? GetDiskSpaceUsage();

    /// <summary>
    /// Gets the system uptime as a formatted string.
    /// </summary>
    /// <returns>A string representing the system uptime.</returns>
    string GetSystemUptime();

    /// <summary>
    /// Gets the total processor time used by the system.
    /// </summary>
    /// <returns>A double representing the total processor time in seconds.</returns>
    double GetTotalProcessorTime();

    /// <summary>
    /// Gets the available working set memory of the system.
    /// </summary>
    /// <returns>A long representing the available working set memory in bytes.</returns>
    long GetAvailableWorkingSetMemory();
}
