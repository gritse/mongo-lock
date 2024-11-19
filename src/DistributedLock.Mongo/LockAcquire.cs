using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable ClassNeverInstantiated.Global

namespace DistributedLock.Mongo;

/// <summary>
/// 锁定实体
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
    /// 过期时间
    /// </summary>
    public DateTime ExpiresIn { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Acquired { get; set; }

    /// <summary>
    /// 锁ID
    /// </summary>
    public Guid AcquireId { get; set; }
}