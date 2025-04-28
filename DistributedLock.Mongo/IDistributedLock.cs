using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedLock.Mongo
{
    public interface IDistributedLock
    {
        Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout, CancellationToken cancellationToken = default);
        Task ReleaseAsync(IAcquire acquire, CancellationToken cancellationToken = default);
    }
}