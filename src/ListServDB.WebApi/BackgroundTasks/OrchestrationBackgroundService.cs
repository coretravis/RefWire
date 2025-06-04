using ListServDB.Core.API;
using ListServDB.Core.HA;
using ListServDB.Orchestration;
using ListServDB.WebApi.Utilities;

namespace ListServDB.WebApi.BackgroundTasks;

/// <summary>
/// Background service for periodically polling distributed updates.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrchestrationBackgroundService"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="listServApi">The ListServ API instance.</param>
/// <param name="healthReportManager">The health report manager instance.</param>
/// <param name="distributedAppInstanceManager">The distributed app instance manager instance.</param>
/// <param name="pollingInterval">The polling interval.</param>
public class OrchestrationBackgroundService(
    ILogger<OrchestrationBackgroundService> logger,
    IListServApi listServApi,
    IHealthReportManager healthReportManager,
    IDistributedInstanceManager distributedAppInstanceManager,
    TimeSpan? pollingInterval = null) : BackgroundService
{
    private readonly ILogger<OrchestrationBackgroundService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDistributedInstanceManager _distributedAppInstanceManager = distributedAppInstanceManager ?? throw new ArgumentNullException(nameof(distributedAppInstanceManager));
    private readonly TimeSpan _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(30);
    private readonly IListServApi _listServApi = listServApi ?? throw new ArgumentNullException(nameof(listServApi));
    private readonly IHealthReportManager _healthReportManager = healthReportManager ?? throw new ArgumentNullException(nameof(healthReportManager));

    // Configuration constants
    private const int InitialStartupDelaySeconds = 5;

    /// <summary>
    /// Executes the background service asynchronously.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to terminate the service</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var appInstance = await RegisterApplicationInstanceAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting distributed manager background process");

                await EnsureLeaderExistsAsync();

                if (await _distributedAppInstanceManager.IsLeader())
                {
                    await PerformLeaderTasksAsync();
                }

                await ReportInstanceHealthAsync(appInstance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApiKeyFeatureDisabledMessage during distributed update polling");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Registers the current application instance with the orchestration system
    /// </summary>
    private async Task<AppInstance> RegisterApplicationInstanceAsync()
    {
        // Generate a unique name for the application instance
        var uniqueName = SimpleNameGenerator.GenerateName();

        _logger.LogInformation("Registering application instance with name: {UniqueName}", uniqueName);

        // Register the application instance with the distributed app instance manager
        var appHost = $"listserv://{uniqueName}";
        return await _distributedAppInstanceManager.RegisterAppInstanceAsync(uniqueName, appHost);
    }

    /// <summary>
    /// Ensures a leader exists in the distributed system, attempting to become leader if none exists
    /// </summary>
    private async Task EnsureLeaderExistsAsync()
    {
        // Check if a leader exists
        var instances = await _distributedAppInstanceManager.GetAppInstancesAsync();
        var hasLeader = instances.Any(i => i.IsLeader && i.IsAlive);

        // If no leader is found, attempt to become one
        if (!hasLeader)
        {
            _logger.LogInformation("No leader found, attempting to become leader");
            await _distributedAppInstanceManager.TryBecomeLeader();
        }
    }

    /// <summary>
    /// Performs tasks that only the leader instance should handle
    /// </summary>
    private async Task PerformLeaderTasksAsync()
    {
        _logger.LogInformation("This instance is the leader");

        // Remove dead instances
        var instances = await _distributedAppInstanceManager.GetAppInstancesAsync();
        var unhealthyInstances = instances.Where(i => !i.IsAlive).ToList();

        if (unhealthyInstances.Count > 0)
        {
            _logger.LogInformation("Removing {UnhealthyCount} unhealthy instances", unhealthyInstances.Count);
            await _distributedAppInstanceManager.RemoveAppInstancesAsync(unhealthyInstances.Select(i => i.Id).ToList());
        }

        _logger.LogInformation("Completed leader tasks");
    }

    /// <summary>
    /// Reports health metrics for the current instance
    /// </summary>
    private async Task ReportInstanceHealthAsync(AppInstance appInstance)
    {
        _logger.LogInformation("Reporting health of {AppName} || {AppId} instance", appInstance.AppName, appInstance.Id);

        // Update the instance's heartbeat
        var state = await _listServApi.GetStateAsync();
        var datasets = state.Datasets ?? new Dictionary<string, Core.Models.Dataset>();
        var healthReport = _healthReportManager.GetHealthReport(datasets);

        // Update the instance's health metrics
        var currentInstance = await _distributedAppInstanceManager.GetThisInstanceAsync();
        currentInstance.UpdateMetrics(
            healthReport.TotalDatasets,
            healthReport.TotalManagedMemoryKB,
            healthReport.TotalProcessorTimeMs,
            healthReport.CPUUsage ?? 0,
            healthReport.DiskSpaceUsage ?? 0,
            healthReport.Timestamp);

        // Report the updated instance status
        await _distributedAppInstanceManager.ReportStatus(currentInstance.Id, currentInstance);
        _logger.LogInformation("Completed health reporting for {AppName} || {AppId}", appInstance.AppName, appInstance.Id);
    }
}
