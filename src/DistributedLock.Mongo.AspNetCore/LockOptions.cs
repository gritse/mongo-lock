// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace DistributedLock.Mongo.AspNetCore;

/// <summary>
/// Lock Options
/// </summary>
public sealed class LockOptions
{
    /// <summary>
    /// db name
    /// </summary>
    public string? DatabaseName { get; set; } = "easily_lock";

    /// <summary>
    /// signal collection name
    /// </summary>
    public string? SignalCollName { get; set; } = "lock.release.signal";

    /// <summary>
    /// acquire collection name
    /// </summary>
    public string? AcquireCollName { get; set; } = "lock.acquire";

    /// <summary>
    /// document count, default: 10,000
    /// </summary>
    public long? MaxDocument { get; set; } = 10_000;

    /// <summary>
    /// maxsize, default: 370000KB
    /// </summary>
    public long? MaxSize { get; set; } = 370_000;
}