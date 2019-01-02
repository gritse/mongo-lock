using MongoDB.Bson.Serialization.Attributes;
using System;


namespace DistributedLock.Mongo
{
    public class LockAcquire<T>
    {
        [BsonId]
        public T Id { get; set; }
        public DateTime ExpiresIn { get; set; }
        public bool Acquired { get; set; }
        public Guid AcquireId { get; set; }
    }
}
