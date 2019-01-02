using System;

namespace DistributedLock.Mongo
{
    public interface IAcquire
    {
        /// <summary>
        /// true if lock successfully acquired; otherwise, false
        /// </summary>
        bool Acquired { get; }
        Guid AcquireId { get; }
    }
}
