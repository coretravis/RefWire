using Microsoft.Extensions.Logging;
using System;

namespace ListServDB.Logging;

/// <summary>
/// A helper class that wraps Microsoft.Extensions.Logging for ListServ.
/// Provides convenience methods for structured logging.
/// </summary>
public class ListServLogger(ILogger logger)
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void LogTrace(string message, params object[] args)
    {
        _logger.LogTrace(message, args);
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogError(Exception ex, string message, params object[] args)
    {
        _logger.LogError(ex, message, args);
    }

    public void LogCritical(Exception ex, string message, params object[] args)
    {
        _logger.LogCritical(ex, message, args);
    }

    /// <summary>
    /// Begins an operation scope to group log messages for a particular operation.
    /// </summary>
    public IDisposable BeginOperationScope(string operationName, params object[] args)
    {
        _logger.LogInformation("Operation {OperationName} started.", operationName);
        return _logger.BeginScope("Operation: {OperationName}", operationName)
            ?? throw new InvalidOperationException("Failed to begin operation scope.");
    }
}
