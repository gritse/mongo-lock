namespace DistributedLock.Mongo;

/// <summary>
/// IDistributedLock
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    ///  Attempts, for the specified amount of time, to acquire an exclusive lock
    /// </summary>
    /// <param name="lifetime">A TimeSpan representing the amount of time after which the lock is automatically released</param>
    /// <param name="timeout">A TimeSpan representing the amount of time to wait for the lock</param>
    /// <returns></returns>
    Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout);

    /// <summary>
    ///  Releases an exclusive lock for the specified acquire. If lock isn't exist or already released, there will be no exceptions throwed
    /// </summary>
    /// <param name="acquire">IAcquire object returned by AcquireAsync</param>
    /// <returns></returns>
    Task ReleaseAsync(IAcquire acquire);
}