namespace DistributedLock.Mongo;

/// <inheritdoc />
internal sealed class AcquireResult : IAcquire
{
    private readonly IDistributedLock? _distributedLock;

    /// <summary>
    /// create a new instance of AcquireResult
    /// </summary>
    /// <param name="acquireId"></param>
    /// <param name="distributedLock"></param>
    public AcquireResult(Guid acquireId, IDistributedLock distributedLock)
    {
        Acquired = true;
        AcquireId = acquireId;
        _distributedLock = distributedLock;
    }

    /// <summary>
    /// create a new instance of AcquireResult
    /// </summary>
    public AcquireResult()
    {
        Acquired = false;
    }

    /// <inheritdoc />
    public bool Acquired { get; }

    /// <inheritdoc />
    public Guid AcquireId { get; }

    public async ValueTask DisposeAsync()
    {
        if (Acquired && _distributedLock is not null)
        {
            await _distributedLock.ReleaseAsync(this);
        }
    }
}