using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable ClassNeverInstantiated.Global

namespace DistributedLock.Mongo;

/// <summary>
/// Lock acquire
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class LockAcquire<T>
{
    /// <summary>
    /// Id
    /// </summary>
    [BsonId]
    public T? Id { get; set; }
    /// <summary>
    /// Expires time
    /// </summary>
    public DateTime ExpiresIn { get; set; }

    /// <summary>
    /// Successfully acquired
    /// </summary>
    public bool Acquired { get; set; }

    /// <summary>
    /// Acquire id
    /// </summary>
    public Guid AcquireId { get; set; }
}