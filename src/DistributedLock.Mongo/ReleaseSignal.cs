using MongoDB.Bson.Serialization.Attributes;

namespace DistributedLock.Mongo;

/// <summary>
/// 释放信号
/// </summary>
public sealed class ReleaseSignal
{
    /// <summary>
    /// 锁ID
    /// </summary>
    [BsonId]
    public Guid AcquireId { get; set; }
}