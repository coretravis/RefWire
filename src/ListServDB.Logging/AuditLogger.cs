using Microsoft.Extensions.Logging;

namespace ListServDB.Logging;

/// <summary>
/// A simple console logger implementing Microsoft.Extensions.Logging-like functionality.
/// </summary>
public class ConsoleLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{DateTime.UtcNow}] [{logLevel}] {formatter(state, exception)}");
    }
}
