using MongoDB.Bson.Serialization.Attributes;

namespace DistributedLock.Mongo;

/// <summary>
/// release signal
/// </summary>
public sealed class ReleaseSignal
{
    /// <summary>
    /// id
    /// </summary>
    [BsonId]
    public Guid AcquireId { get; set; }
}