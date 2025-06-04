namespace ListServDB.Core.Concurrency;

/// <summary>
/// Provides a simple wrapper around ReaderWriterLockSlim for managing concurrent access.
/// </summary>
public class ConcurrencyManager : IDisposable, IConcurrencyManager
{
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public void EnterReadLock() => _lock.EnterReadLock();
    public void ExitReadLock() => _lock.ExitReadLock();
    public void EnterWriteLock() => _lock.EnterWriteLock();
    public void ExitWriteLock() => _lock.ExitWriteLock();
    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
