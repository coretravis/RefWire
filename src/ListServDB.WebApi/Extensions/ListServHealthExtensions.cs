using ListServDB.Core.HA;
using ListServDB.Core.HA.Health;
using ListServDB.Core.HA.Metrics;
using System.Runtime.InteropServices;

namespace ListServDB.WebApi.Extensions;

/// <summary>
/// Extension methods for adding ListServ metrics to the service collection.
/// </summary>
public static class ListServMetricsExtensions
{
    /// <summary>
    /// Adds the appropriate health metrics provider to the service collection based on the operating system.
    /// </summary>
    /// <param name="services">The service collection to add the metrics provider to.</param>
    /// <returns>The updated service collection.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the operating system is not supported.</exception>
    public static IServiceCollection AddListServMetrics(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IHealthMetricsProvider, WindowsHealthMetricsProvider>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            services.AddSingleton<IHealthMetricsProvider, MacOsHealthMetricsProvider>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<IHealthMetricsProvider, LinuxHealthMetricsProvider>();
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }
        services.AddSingleton<IHealthReportManager, HealthReportManager>();
        return services;
    }
}
