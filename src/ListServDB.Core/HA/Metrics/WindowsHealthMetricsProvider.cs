using System.Runtime.Versioning;

namespace ListServDB.Core.HA.Metrics;

[SupportedOSPlatform("windows")]
public class WindowsHealthMetricsProvider : IHealthMetricsProvider
{
    public float? GetCpuUsage()
    {
        using var cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
        cpuCounter.NextValue();
        System.Threading.Thread.Sleep(1000);
        return cpuCounter.NextValue();
    }

    public float? GetDiskSpaceUsage()
    {
        using var diskCounter = new System.Diagnostics.PerformanceCounter("LogicalDisk", "% Free Space", "_Total");
        return 100 - diskCounter.NextValue();
    }

    public string GetSystemUptime()
    {
        using var uptime = new System.Diagnostics.PerformanceCounter("System", "System Up Time");
        uptime.NextValue();
        TimeSpan uptimeSpan = TimeSpan.FromSeconds(uptime.NextValue());
        return $"{uptimeSpan.Days} days, {uptimeSpan.Hours} hours, {uptimeSpan.Minutes} minutes, {uptimeSpan.Seconds} seconds";
    }

    public double GetTotalProcessorTime()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        return process.TotalProcessorTime.TotalMilliseconds;
    }

    public long GetAvailableWorkingSetMemory()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        return process.WorkingSet64 / 1024;
    }
}
