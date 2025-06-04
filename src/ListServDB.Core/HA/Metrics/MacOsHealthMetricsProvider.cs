using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ListServDB.Core.HA.Metrics;

[SupportedOSPlatform("macos")]
public class MacOsHealthMetricsProvider : IHealthMetricsProvider
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
            DateTime bootTime = GetBootTime();
            if (bootTime != DateTime.MinValue)
            {
                TimeSpan uptime = DateTime.UtcNow - bootTime;
                return $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes, {uptime.Seconds} seconds";
            }
        }
        catch (Exception)
        {
            // Log exception if needed
        }
        return "Unavailable";
    }

    private static DateTime GetBootTime()
    {
        // Use sysctl with "kern.boottime" to get boot time.
        int[] mib = new int[] { CTL_KERN, KERN_BOOTTIME };
        int size = Marshal.SizeOf<TimeVal>();
        int ret = sysctl(mib, (uint)mib.Length, out TimeVal boottime, ref size, IntPtr.Zero, 0);
        if (ret != 0)
        {
            throw new InvalidOperationException("sysctl failed to get kern.boottime");
        }
        // Convert seconds since epoch to DateTime.
        return DateTimeOffset.FromUnixTimeSeconds(boottime.tv_sec).UtcDateTime;
    }

    // Constants for sysctl call.
    private const int CTL_KERN = 1;
    private const int KERN_BOOTTIME = 21;

    [StructLayout(LayoutKind.Sequential)]
    private struct TimeVal
    {
        public long tv_sec;   // seconds
        public int tv_usec;   // microseconds
    }

    [DllImport("libc")]
    private static extern int sysctl(int[] name, uint namelen, out TimeVal oldp, ref int oldlenp, IntPtr newp, uint newlen);

    public float? GetCpuUsage()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sh",
                Arguments = "-c \"top -l 2 -n 0 | grep 'CPU usage' | tail -1\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                int idleIndex = output.IndexOf("idle", StringComparison.OrdinalIgnoreCase);
                if (idleIndex > 0)
                {
                    int start = output.LastIndexOf(' ', idleIndex - 1);
                    if (start >= 0)
                    {
                        string idleStr = output[start..idleIndex].Trim();
                        idleStr = idleStr.Replace("%", "");
                        if (float.TryParse(idleStr, out float idle))
                        {
                            return 100 - idle;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Log exception if needed
        }
        return null;
    }

    public float? GetDiskSpaceUsage()
    {
        try
        {
            // On macOS, the root ("/") drive typically represents the main disk.
            var drive = new DriveInfo("/");
            if (drive.IsReady)
            {
                long totalSpace = drive.TotalSize;
                long freeSpace = drive.TotalFreeSpace;
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
