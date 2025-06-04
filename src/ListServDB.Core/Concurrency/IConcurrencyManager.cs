namespace ListServDB.Core.Concurrency;

/// <summary>
/// Interface for concurrency manager
/// </summary>
public interface IConcurrencyManager
{
    /// <summary>
    /// Releases all resources used by the concurrency manager.
    /// </summary>
    void Dispose();

    /// <summary>
    /// Acquires a read lock.
    /// </summary>
    void EnterReadLock();

    /// <summary>
    /// Acquires a write lock.
    /// </summary>
    void EnterWriteLock();

    /// <summary>
    /// Releases a read lock.
    /// </summary>
    void ExitReadLock();

    /// <summary>
    /// Releases a write lock.
    /// </summary>
    void ExitWriteLock();
}
