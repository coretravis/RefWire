namespace ListServDB.Core.HA.Metrics;

using ListServDB.Core.HA;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;

[SupportedOSPlatform("linux")]
public class LinuxHealthMetricsProvider : IHealthMetricsProvider
{
    public double GetTotalProcessorTime()
    {
        using var process = Process.GetCurrentProcess();
        return process.TotalProcessorTime.TotalMilliseconds;
    }

    public long GetAvailableWorkingSetMemory()
    {
        using var process = Process.GetCurrentProcess();
        return process.WorkingSet64 / 1024;
    }

    public string GetSystemUptime()
    {
        try
        {
            // /proc/uptime returns a string like "12345.67 54321.89"
            string content = File.ReadAllText("/proc/uptime");
            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && double.TryParse(parts[0], out double uptimeSeconds))
            {
                TimeSpan uptime = TimeSpan.FromSeconds(uptimeSeconds);
                return $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes, {uptime.Seconds} seconds";
            }
        }
        catch (Exception)
        {
            // Log exception if needed
        }
        return "Unavailable";
    }

    public float? GetCpuUsage()
    {
        try
        {
            // Read initial CPU statistics.
            var cpu1 = ReadCpuStat();
            Thread.Sleep(1000);
            // Read CPU statistics again after 1 second.
            var cpu2 = ReadCpuStat();

            if (cpu1.HasValue && cpu2.HasValue)
            {
                long idleDiff = cpu2.Value.Idle - cpu1.Value.Idle;
                long totalDiff = cpu2.Value.Total - cpu1.Value.Total;
                if (totalDiff > 0)
                {
                    // Calculate usage as percentage (100% minus idle time percentage).
                    return (float)(100.0 * (totalDiff - idleDiff) / totalDiff);
                }
            }
        }
        catch (Exception)
        {
            // Log exception if needed
        }
        return null;
    }

    private static (long Idle, long Total)? ReadCpuStat()
    {
        // The first line in /proc/stat looks like:
        // cpu  3357 0 4313 1362393 0 0 0 0 0 0
        string[] lines = File.ReadAllLines("/proc/stat");
        if (lines.Length > 0 && lines[0].StartsWith("cpu "))
        {
            var parts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 5)
            {
                // According to /proc/stat:
                // parts[1]=user, parts[2]=nice, parts[3]=system, parts[4]=idle, parts[5]=iowait, etc.
                long total = 0;
                long idle = 0;
                for (int i = 1; i < parts.Length; i++)
                {
                    if (long.TryParse(parts[i], out long value))
                    {
                        total += value;
                        // The 4th token (index 4) is idle.
                        if (i == 4)
                        {
                            idle = value;
                        }
                    }
                }
                return (Idle: idle, Total: total);
            }
        }
        return null;
    }

    public float? GetDiskSpaceUsage()
    {
        try
        {
            // On Linux, the root ("/") drive is typically the main drive.
            var drive = new DriveInfo("/");
            if (drive.IsReady)
            {
                long totalSpace = drive.TotalSize;
                long freeSpace = drive.TotalFreeSpace;
                // Calculate usage percentage.
                return (float)(100.0 * (totalSpace - freeSpace) / totalSpace);
            }
        }
        catch (Exception)
        {
            // Log exception if needed
        }
        return null;
    }
}
