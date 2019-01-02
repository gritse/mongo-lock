using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DistributedLock.Mongo
{
    public class ReleaseSignal
    {
        [BsonId]
        public Guid AcquireId { get; set; }
    }
}
