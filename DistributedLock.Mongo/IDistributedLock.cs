using System;
using System.Threading.Tasks;

namespace DistributedLock.Mongo
{
    public interface IDistributedLock
    {
        Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout);
        Task ReleaseAsync(IAcquire acquire);
    }
}