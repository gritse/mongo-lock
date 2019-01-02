using System;

namespace DistributedLock.Mongo
{
    internal class AcquireResult : IAcquire
    {
        public AcquireResult(Guid acquireId)
        {
            Acquired = true;
            AcquireId = acquireId;
        }

        public AcquireResult()
        {
            Acquired = false;
        }

        public bool Acquired { get; private set; }

        public Guid AcquireId { get; private set; }
    }
}
