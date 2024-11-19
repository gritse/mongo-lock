namespace DistributedLock.Mongo;

/// <summary>
/// IAcquire
/// </summary>
public interface IAcquire : IAsyncDisposable
{
    /// <summary>
    /// true if lock successfully acquired; otherwise, false
    /// </summary>
    bool Acquired { get; }

    /// <summary>
    /// ID
    /// </summary>
    Guid AcquireId { get; }
}