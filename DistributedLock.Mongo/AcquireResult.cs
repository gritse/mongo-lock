using System;
using System.Threading.Tasks;

namespace DistributedLock.Mongo
{
    internal class AcquireResult : IAcquire
    {
        private readonly IDistributedLock _distributedLock;

        public AcquireResult(Guid acquireId, IDistributedLock distributedLock)
        {
            _distributedLock = distributedLock;
            Acquired = true;
            AcquireId = acquireId;
        }

        public AcquireResult()
        {
            Acquired = false;
        }

        public bool Acquired { get; private set; }

        public Guid AcquireId { get; private set; }

        public async ValueTask DisposeAsync()
        {
            if (Acquired && _distributedLock != null)
            {
                await _distributedLock.ReleaseAsync(this);
            }
        }
    }
}
