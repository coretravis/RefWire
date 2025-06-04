namespace ListServDB.Orchestration;

/// <summary>
/// Represents an application instance.
/// </summary>
public class AppInstance
{
    /// <summary>
    /// Gets or sets the unique identifier for the application instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the time at which the instance was instantiated.
    /// </summary>
    public DateTime InstantiatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the host name where the instance is running.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the application.
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this instance is the leader.
    /// </summary>
    public bool IsLeader { get; set; }

    /// <summary>
    /// Gets the running time since the instance was instantiated.
    /// </summary>
    public TimeSpan RunningTime => DateTime.UtcNow - InstantiatedAt;

    /// <summary>
    /// Gets or sets the time of the last heartbeat signal.
    /// </summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the duration since the last heartbeat.
    /// </summary>
    public TimeSpan TimeSinceLastHeartbeat => DateTime.UtcNow - LastHeartbeat;

    /// <summary>
    /// Gets a value indicating whether the instance is considered alive.
    /// </summary>
    public bool IsAlive => TimeSinceLastHeartbeat < TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the number of loaded datasets.
    /// </summary>
    public long LoadedDatasets { get; set; }

    /// <summary>
    /// Gets or sets the managed memory usage.
    /// </summary>
    public long MangedMemory { get; set; }

    /// <summary>
    /// Gets or sets the total CPU time.
    /// </summary>
    public double TotalCpuTime { get; set; }

    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public float CpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the disk usage percentage.
    /// </summary>
    public float DiskUsage { get; set; }

    /// <summary>
    /// Updates the metrics of the application instance.
    /// </summary>
    /// <param name="loadedDatasets">The number of loaded datasets.</param>
    /// <param name="mangedMemory">The managed memory usage.</param>
    /// <param name="totalCpuTime">The total CPU time.</param>
    /// <param name="cpuUsage">The CPU usage percentage.</param>
    /// <param name="diskUsage">The disk usage percentage.</param>
    /// <param name="lastHeartbeatTime">The time of the last heartbeat.</param>
    public void UpdateMetrics(long loadedDatasets,
                              long mangedMemory,
                              double totalCpuTime,
                              float cpuUsage,
                              float diskUsage,
                              DateTime lastHeartbeatTime)
    {
        LoadedDatasets = loadedDatasets;
        MangedMemory = mangedMemory;
        TotalCpuTime = totalCpuTime;
        CpuUsage = cpuUsage;
        DiskUsage = diskUsage;
        LastHeartbeat = lastHeartbeatTime;
    }

    /// <summary>
    /// Creates a new copy of the current application instance.
    /// </summary>
    /// <returns>A clone of the current application instance.</returns>
    public AppInstance Clone()
    {
        return new AppInstance
        {
            Id = Id,
            InstantiatedAt = InstantiatedAt,
            HostName = HostName,
            AppName = AppName,
            IsLeader = IsLeader,
            LastHeartbeat = LastHeartbeat,
            LoadedDatasets = LoadedDatasets,
            MangedMemory = MangedMemory,
            TotalCpuTime = TotalCpuTime,
            CpuUsage = CpuUsage,
            DiskUsage = DiskUsage
        };
    }
}
