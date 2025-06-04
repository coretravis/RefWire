using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ListServDB.Orchestration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Diagnostics;
using System.Text.Json;

namespace ListServDb.Orchestration.Azure.Blob;

/// <summary>
/// App instance manager using Azure Blob Storage
/// </summary>
public class AzureBlobDistributedInstanceManager : IDistributedInstanceManager, IDisposable
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobDistributedInstanceManager> _logger;
    private readonly DistributedInstanceManagerConfig _config;

    // Circuit breaker policies
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncPolicy _combinedPolicy;

    // Storage paths
    private const string APP_INSTANCES_BLOB = "appinstances.json";
    private const string MESSAGES_BLOB_PREFIX = "messages/";

    // Instance state
    private bool _isLeader = false;
    private AppInstance? _thisInstance;

    public AzureBlobDistributedInstanceManager(
        ILogger<AzureBlobDistributedInstanceManager> logger,
        string connectionString,
        DistributedInstanceManagerConfig? config = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? new DistributedInstanceManagerConfig();

        try
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(config?.ContainerName);
            _containerClient.CreateIfNotExists();

            // Configure circuit breaker policy
            _circuitBreakerPolicy = Policy
                .Handle<Exception>(ex => ShouldHandleException(ex))
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: _config.CircuitBreakerFailureThreshold,
                    durationOfBreak: _config.CircuitBreakerResetTimeout,
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogWarning("Circuit breaker triggered. Blocking all requests for {Duration}. Reason: {Exception}", duration, exception.Message);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset. Requests will now be processed.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker is half-open. Next call will test system stability.");
                    }
                );

            // Configure retry policy with specific exception handling
            _retryPolicy = Policy
                .Handle<Exception>(ex => ShouldRetryOnException(ex))
                .WaitAndRetryAsync(
                    _config.RetryDelays,
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, "Operation failed. Retry {RetryCount} in {Delay}. Context: {Context}", retryCount, timeSpan, context);
                    }
                );

            _combinedPolicy = Policy.WrapAsync(_circuitBreakerPolicy, _retryPolicy);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize AzureBlobDistributedInstanceManager");
            throw;
        }
    }

    public async Task<AppInstance> RegisterAppInstanceAsync(string appName, string hostName)
    {
        string operationId = Guid.NewGuid().ToString();
        return await ExecuteWithResilienceAsync<AppInstance>(async () =>
        {
            return await PerformWithLeaseAsync<AppInstance>(APP_INSTANCES_BLOB, async (blobClient, leaseClient) =>
            {
                // Ensure the blob exists
                var existingInstances = await ReadBlobContentAsync(blobClient, new List<AppInstance>());

                var newInstance = new AppInstance
                {
                    AppName = appName,
                    HostName = hostName,
                    IsLeader = existingInstances.Count == 0 || existingInstances.All(i => !i.IsLeader)
                };

                // set this app instance as the leader
                if (existingInstances.Count == 0 || existingInstances.All(i => !i.IsLeader))
                {
                    _isLeader = true;
                }

                existingInstances.Add(newInstance);

                // Set this instance as the current instance
                _thisInstance = newInstance.Clone();

                // Add operation ID for idempotency
                await blobClient.UploadAsync(
                    new BinaryData(JsonSerializer.Serialize(existingInstances)),
                    new BlobUploadOptions
                    {
                        Conditions = new BlobRequestConditions { LeaseId = leaseClient.LeaseId },
                        Metadata = new Dictionary<string, string> { { "OperationId", operationId } }
                    }
                );

                _logger.LogInformation("Registered new app instance: {InstanceId} for app {AppName} on host {HostName}",
                    newInstance.Id, appName, hostName);
                return newInstance;
            });
        }, operationId);
    }

    public async Task<IEnumerable<AppInstance>> GetAppInstancesAsync()
    {
        return await ExecuteWithResilienceAsync<IEnumerable<AppInstance>>(async () =>
        {
            var blobClient = _containerClient.GetBlobClient(APP_INSTANCES_BLOB);

            if (!await blobClient.ExistsAsync())
            {
                _logger.LogInformation("No app instances blob exists.");
                return Enumerable.Empty<AppInstance>();
            }

            var instances = await ReadBlobContentAsync(blobClient, new List<AppInstance>());
            _logger.LogInformation("Retrieved {InstanceCount} app instances", instances.Count);
            return instances;
        });
    }

    public async Task SendMessageAsync(string messageContent)
    {
        if (_thisInstance == null)
        {
            throw new InvalidOperationException("This instance is not registered.");
        }

        var allInstancesExceptThis = (await GetAppInstancesAsync()).Where(i => i.Id != _thisInstance.Id).ToList();
        var failedInstances = new List<(Guid instanceId, Exception exception)>();

        // Process instances in parallel with individual error handling
        var sendTasks = allInstancesExceptThis.Select(async instance =>
        {
            try
            {
                await SendMessageToInstanceAsync(instance, messageContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to instance {InstanceId}", instance.Id);
                failedInstances.Add((instance.Id, ex));
            }
        });

        await Task.WhenAll(sendTasks);

        if (failedInstances.Count > 0)
        {
            _logger.LogWarning("Failed to send message to {FailedCount} out of {TotalCount} instances",
                failedInstances.Count, allInstancesExceptThis.Count);
        }
    }

    public async Task<IEnumerable<Message>> ReadMessagesAsync(Guid instanceId)
    {
        return await ExecuteWithResilienceAsync<IEnumerable<Message>>(async () =>
        {
            var messageBlobPath = $"{MESSAGES_BLOB_PREFIX}{instanceId}.json";
            var messageBlob = _containerClient.GetBlobClient(messageBlobPath);

            if (!await messageBlob.ExistsAsync())
            {
                _logger.LogInformation("No messages found for instance {InstanceId}", instanceId);
                return Enumerable.Empty<Message>();
            }

            return await PerformWithLeaseAsync<IEnumerable<Message>>(messageBlobPath, async (blobClient, leaseClient) =>
            {
                var messages = await ReadBlobContentAsync(blobClient, new List<Message>());

                // Clear the messages by uploading an empty array instead of deleting the blob
                // This is more idempotent in case of retries
                await blobClient.UploadAsync(
                    new BinaryData(JsonSerializer.Serialize(new List<Message>())),
                    new BlobUploadOptions { Conditions = new BlobRequestConditions { LeaseId = leaseClient.LeaseId } }
                );

                _logger.LogInformation("Read and cleared {MessageCount} messages for instance {InstanceId}",
                    messages.Count, instanceId);
                return messages;
            });
        });
    }

    public async Task<AppInstance> ReportStatus(Guid instanceId, AppInstance appInstance)
    {
        return await ExecuteWithResilienceAsync<AppInstance>(async () =>
        {
            return await PerformWithLeaseAsync<AppInstance>(APP_INSTANCES_BLOB, async (blobClient, leaseClient) =>
            {
                var instances = await ReadBlobContentAsync(blobClient, new List<AppInstance>());

                var instance = instances.FirstOrDefault(i => i.Id == instanceId)
                ?? throw new KeyNotFoundException($"App instance with ID {instanceId} not found.");

                instance.UpdateMetrics(
                    appInstance.LoadedDatasets,
                    appInstance.MangedMemory,
                    appInstance.TotalCpuTime,
                    appInstance.CpuUsage,
                    appInstance.DiskUsage,
                    appInstance.LastHeartbeat
                );

                await blobClient.UploadAsync(
                    new BinaryData(JsonSerializer.Serialize(instances)),
                    new BlobUploadOptions { Conditions = new BlobRequestConditions { LeaseId = leaseClient.LeaseId } }
                );

                // Update local instance state
                if (_thisInstance != null && instanceId == _thisInstance.Id)
                {
                    _thisInstance = instance.Clone();
                }

                _logger.LogInformation("Updated metrics for app instance {InstanceId}", instanceId);
                return instance;
            });
        });
    }

    public Task<AppInstance> GetThisInstanceAsync()
    {
        return Task.FromResult(_thisInstance!);
    }

    public Task<bool> IsLeader()
    {
        return Task.FromResult(_isLeader);
    }

    public async Task RemoveAppInstanceAsync(Guid instanceId)
    {
        await ExecuteWithResilienceAsync<object>(async () =>
        {
            await RemoveInstancesAsync(new List<Guid> { instanceId });
            return Task.CompletedTask;
        });
    }

    public async Task RemoveAppInstancesAsync(List<Guid> instanceIds)
    {
        await ExecuteWithResilienceAsync<object>(async () =>
        {
            await RemoveInstancesAsync(instanceIds);
            return Task.CompletedTask;
        });
    }

    public async Task<bool> TryBecomeLeader()
    {
        return await ExecuteWithResilienceAsync<bool>(async () =>
        {
            return await PerformWithLeaseAsync<bool>(APP_INSTANCES_BLOB, async (blobClient, leaseClient) =>
            {
                var instances = await ReadBlobContentAsync(blobClient, new List<AppInstance>());

                var targetInstance = instances.FirstOrDefault(i => i.Id == _thisInstance?.Id);
                if (targetInstance == null)
                {
                    return false;
                }
                if (targetInstance == null)
                {
                    return false;
                }

                // Set all instances as non-leader then mark this instance as leader.
                foreach (var instance in instances)
                {
                    instance.IsLeader = false;
                }
                targetInstance.IsLeader = true;

                // Update local state.
                _isLeader = true;
                _thisInstance = targetInstance.Clone();

                await blobClient.UploadAsync(
                    new BinaryData(JsonSerializer.Serialize(instances)),
                    new BlobUploadOptions { Conditions = new BlobRequestConditions { LeaseId = leaseClient.LeaseId } }
                );

                _logger.LogInformation("App instance {InstanceId} volunteered for leadership and is now leader", _thisInstance.Id);
                return true;
            });
        });
    }

    private async Task RemoveInstancesAsync(List<Guid> instanceIds)
    {
        await PerformWithLeaseAsync<object>(APP_INSTANCES_BLOB, async (blobClient, leaseClient) =>
        {
            var existingInstances = await ReadBlobContentAsync(blobClient, new List<AppInstance>());

            bool isRemovingLeader = existingInstances.Any(i => instanceIds.Contains(i.Id) && i.IsLeader);
            var removedCount = existingInstances.RemoveAll(i => instanceIds.Contains(i.Id));

            // Reassign leadership if necessary
            if (isRemovingLeader && existingInstances.Count > 0)
            {
                existingInstances.First().IsLeader = true;
                _logger.LogInformation("Reassigned leadership to instance {NewLeaderId}", existingInstances.First().Id);
            }

            await blobClient.UploadAsync(
                new BinaryData(JsonSerializer.Serialize(existingInstances)),
                new BlobUploadOptions { Conditions = new BlobRequestConditions { LeaseId = leaseClient.LeaseId } }
            );

            _logger.LogInformation("Removed {RemovedCount} instance(s) with IDs {InstanceIds}",
                removedCount, string.Join(", ", instanceIds));

            // Clear messages for the removed instances
            await ClearInstanceMessagesIndividuallyAsync(instanceIds);

            return Task.CompletedTask;
        });
    }

    private async Task ClearInstanceMessagesIndividuallyAsync(List<Guid> instanceIds)
    {
        if (instanceIds == null || instanceIds.Count == 0)
        {
            _logger.LogInformation("No instance IDs provided for clearing messages.");
            return;
        }

        var deleteTasks = instanceIds.Select(async id =>
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient($"{MESSAGES_BLOB_PREFIX}{id}.json");
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                _logger.LogInformation("Deleted message blob for instance ID: {InstanceId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete message blob for instance ID: {InstanceId}", id);
            }
        });

        await Task.WhenAll(deleteTasks);
        _logger.LogInformation("Completed message blob deletion process for instance IDs: {InstanceIds}",
            string.Join(", ", instanceIds));
    }

    private static bool ShouldHandleException(Exception ex)
    {
        // Exclude transient errors that should only trigger retry policy
        if (ex is TimeoutException ||
            (ex is RequestFailedException storageEx &&
            (storageEx.Status == 429 || storageEx.Status == 503)))
        {
            return false;
        }
        return true;
    }

    private static bool ShouldRetryOnException(Exception ex)
    {
        // Handle transient errors specifically
        if (ex is TimeoutException)
        {
            return true;
        }

        if (ex is RequestFailedException storageEx)
        {
            // Retry on throttling (429), service unavailable (503), connection errors (500)
            int[] retryableStatusCodes = new[] { 429, 500, 503 };
            return retryableStatusCodes.Contains(storageEx.Status);
        }

        // Retry on lease-specific issues
        if (ex.Message.Contains("lease") && ex.Message.Contains("already", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private async Task EnsureBlobExistsAsync(BlobClient blobClient, string defaultContent)
    {
        if (!await blobClient.ExistsAsync())
        {
            _logger.LogInformation("Blob {BlobName} does not exist. Creating new blob with default content.", blobClient.Name);
            await blobClient.UploadAsync(new BinaryData(defaultContent), overwrite: false);
        }
    }

    private async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> operation, string? operationId = null, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = operationId ?? Guid.NewGuid().ToString();

        try
        {
            _logger.LogDebug("Starting operation {OperationName} with correlation ID {CorrelationId}", callerName, correlationId);
            return await _combinedPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error in {MethodName} (correlation ID: {CorrelationId}) after retry attempts. Circuit breaker state: {CircuitState}",
                        callerName, correlationId, _circuitBreakerPolicy.CircuitState);
                    throw;
                }
            });
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Method {MethodName} completed in {ElapsedMilliseconds}ms (correlation ID: {CorrelationId})",
                callerName, stopwatch.ElapsedMilliseconds, correlationId);
        }
    }

    private async Task<BlobLeaseClient> AcquireLeaseWithResilienceAsync(BlobClient blobClient, string? leaseId)
    {
        return await ExecuteWithResilienceAsync(async () =>
        {
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);
            try
            {
                await leaseClient.AcquireAsync(_config.LeaseDuration);
                return leaseClient;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lease acquisition failed for blob: {BlobName}", blobClient.Name);
                throw;
            }
        });
    }

    private async Task<T> PerformWithLeaseAsync<T>(string blobPath, Func<BlobClient, BlobLeaseClient, Task<T>> operation, string defaultContent = "[]")
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);
        await EnsureBlobExistsAsync(blobClient, defaultContent);

        var leaseId = Guid.NewGuid().ToString();
        BlobLeaseClient leaseClient = await AcquireLeaseWithResilienceAsync(blobClient, leaseId);

        try
        {
            return await operation(blobClient, leaseClient);
        }
        finally
        {
            try
            {
                await leaseClient.ReleaseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release lease for blob: {BlobName}", blobClient.Name);
            }
        }
    }

    private static async Task<T> ReadBlobContentAsync<T>(BlobClient blobClient, T defaultValue = default!)
    {
        if (!await blobClient.ExistsAsync())
        {
            return defaultValue;
        }

        var downloadResponse = await blobClient.DownloadContentAsync();
        return JsonSerializer.Deserialize<T>(downloadResponse.Value.Content.ToString()) ?? defaultValue;
    }

    private async Task SendMessageToInstanceAsync(AppInstance instance, string messageContent)
    {
        if (_thisInstance == null)
        {
            throw new InvalidOperationException("This instance is not registered.");
        }

        await PerformWithLeaseAsync<object>($"{MESSAGES_BLOB_PREFIX}{instance.Id}.json", async (messageBlob, leaseClient) =>
        {
            var message = new Message
            {
                FromAppInstanceId = _thisInstance.Id,
                ToAppInstanceId = instance.Id,
                Content = messageContent
            };

            var existingMessages = await ReadBlobContentAsync(messageBlob, new List<Message>());
            existingMessages.Add(message);

            await messageBlob.UploadAsync(
                new BinaryData(JsonSerializer.Serialize(existingMessages)),
                new BlobUploadOptions { Conditions = new BlobRequestConditions { LeaseId = leaseClient.LeaseId } }
            );

            _logger.LogInformation("Sent message from {FromInstanceId} to {ToInstanceId}. Message ID: {MessageId}",
                _thisInstance.Id, instance.Id, message.MessageId);

            return Task.CompletedTask;
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Log disposal
            _logger.LogInformation("AzureBlobDistributedInstanceManager disposed.");
        }
    }
}