namespace ListServDB.Orchestration;

/// <summary>
/// Manages distributed application instances.
/// </summary>
public interface IDistributedInstanceManager
{
    /// <summary>
    /// Registers an application instance asynchronously.
    /// </summary>
    /// <param name="appName">The name of the application.</param>
    /// <param name="hostName">The host name of the application.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the registered <see cref="AppInstance"/>.</returns>
    Task<AppInstance> RegisterAppInstanceAsync(string appName, string hostName);

    /// <summary>
    /// Gets all registered application instances asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AppInstance"/>.</returns>
    Task<IEnumerable<AppInstance>> GetAppInstancesAsync();

    /// <summary>
    /// Reports the status of a specific application instance asynchronously.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="appInstance">The updated application instance details.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see cref="AppInstance"/>.</returns>
    Task<AppInstance> ReportStatus(Guid instanceId, AppInstance appInstance);

    /// <summary>
    /// Gets the current application instance asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current <see cref="AppInstance"/>.</returns>
    Task<AppInstance> GetThisInstanceAsync();

    /// <summary>
    /// Determines if the current instance is the leader asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the current instance is the leader; otherwise, <c>false</c>.</returns>
    Task<bool> IsLeader();

    /// <summary>
    /// Attempts to become the leader asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if successfully became leader; otherwise, <c>false</c>.</returns>
    Task<bool> TryBecomeLeader();

    /// <summary>
    /// Removes a specific application instance asynchronously.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to remove.</param>
    /// <returns>A task that represents the asynchronous removal operation.</returns>
    Task RemoveAppInstanceAsync(Guid instanceId);

    /// <summary>
    /// Removes multiple application instances asynchronously.
    /// </summary>
    /// <param name="instanceIds">A list of instance identifiers to remove.</param>
    /// <returns>A task that represents the asynchronous removal operation.</returns>
    Task RemoveAppInstancesAsync(List<Guid> instanceIds);

    /// <summary>
    /// Sends a message asynchronously.
    /// </summary>
    /// <param name="messageContent">The content of the message to send.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task SendMessageAsync(string messageContent);

    /// <summary>
    /// Reads messages for a specific application instance asynchronously.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the application instance.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="Message"/>.</returns>
    Task<IEnumerable<Message>> ReadMessagesAsync(Guid instanceId);
}
