using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DistributedLock.Mongo
{
    public class MongoLock<T> : IDistributedLock
    {
        private readonly FilterDefinitionBuilder<LockAcquire<T>> _builder = new FilterDefinitionBuilder<LockAcquire<T>>();
        private readonly IMongoCollection<LockAcquire<T>> _locks;
        private readonly IMongoCollection<ReleaseSignal> _signals;
        private readonly T _id;

        /// <summary>
        ///  Initializes a new instance of the MongoLock class with a locks and signals collections and lock identifier
        /// </summary>
        /// <param name="locks">MongoCollection to store locks, shouldn't be a capped collection</param>
        /// <param name="signals">MongoCollection to push signals to release, should be a capped collection</param>
        /// <param name="lock">Identifier of exclusive lock</param>
        public MongoLock(IMongoCollection<LockAcquire<T>> locks, IMongoCollection<ReleaseSignal> signals, T @lock)
        {
            _locks = locks ?? throw new ArgumentNullException(nameof(locks));
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
            _id = @lock;
        }

        /// <summary>
        ///  Attempts, for the specified amount of time, to acquire an exclusive lock
        /// </summary>
        /// <param name="lifetime">A TimeSpan representing the amount of time after which the lock is automatically released</param>
        /// <param name="timeout">A TimeSpan representing the amount of time to wait for the lock</param>
        /// <returns></returns>
        public async Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout)
        {
            if (lifetime < TimeSpan.Zero || lifetime > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(lifetime), "The value of lifetime in milliseconds is negative or is greater than MaxValue");
            if (timeout < TimeSpan.Zero || timeout > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(timeout), "The value of timeout in milliseconds is negative or is greater than MaxValue");

            var acquireId = Guid.NewGuid();

            while (await TryUpdate(lifetime, acquireId) == false)
            {
                using (var cursor = await _locks.FindAsync(_builder.Eq(x => x.Id, _id)))
                {
                    var acquire = await cursor.FirstOrDefaultAsync();

                    if (acquire != null && await WaitSignal(acquire.AcquireId, timeout) == false)
                    {
                        return await TryUpdate(lifetime, acquireId) == false ? new AcquireResult() : new AcquireResult(acquireId, this);
                    }
                }
            }

            return new AcquireResult(acquireId, this);
        }

        /// <summary>
        ///  Releases an exclusive lock for the specified acquire. If lock isn't exist or already released, there will be no exceptions throwed
        /// </summary>
        /// <param name="acquire">IAcquire object returned by AcquireAsync</param>
        /// <returns></returns>
        public async Task ReleaseAsync(IAcquire acquire)
        {
            if (acquire == null) throw new ArgumentNullException(nameof(acquire));
            if (acquire.Acquired == false) return;

            var updateResult = await _locks.UpdateOneAsync(
                filter: _builder.And(_builder.Eq(x => x.Id, _id), _builder.Eq(x => x.AcquireId, acquire.AcquireId)), // x => x.Id == _id && x.AcquireId == acquire.AcquireId,
                update: new UpdateDefinitionBuilder<LockAcquire<T>>().Set(x => x.Acquired, false));

            if (updateResult.IsAcknowledged && updateResult.ModifiedCount > 0)
                await _signals.InsertOneAsync(new ReleaseSignal { AcquireId = acquire.AcquireId });
        }

        private async Task<bool> WaitSignal(Guid acquireId, TimeSpan timeout)
        {
            using (var cursor = await _signals.Find(x => x.AcquireId == acquireId, new FindOptions { MaxAwaitTime = timeout, CursorType = CursorType.TailableAwait }).ToCursorAsync())
            {
                var started = DateTime.UtcNow;

                while (await cursor.MoveNextAsync())
                {
                    if (cursor.Current.Any()) return true;
                    if (DateTime.UtcNow - started >= timeout) return false;
                }

                return false;
            }
        }

        private async Task<bool> TryUpdate(TimeSpan lifetime, Guid acquireId)
        {
            try
            {
                var update = new UpdateDefinitionBuilder<LockAcquire<T>>()
                        .Set(x => x.Acquired, true)
                        .Set(x => x.ExpiresIn, DateTime.UtcNow + lifetime)
                        .Set(x => x.AcquireId, acquireId)
                        .SetOnInsert(x => x.Id, _id);

                var filter = _builder.And(
                    _builder.Eq(x => x.Id, _id), 
                    _builder.Or(
                        _builder.Eq(x => x.Acquired, false),
                        _builder.Lte(x => x.ExpiresIn, DateTime.UtcNow)
                    )
                );

                var updateResult = await _locks.UpdateOneAsync(
                    filter: filter, // x => x.Id == _id && (!x.Acquired || x.ExpiresIn <= DateTime.UtcNow),
                    update: update, options: new UpdateOptions { IsUpsert = true });

                return updateResult.IsAcknowledged;
            }
            catch (MongoWriteException ex) // E11000 
            {
                if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                    return false;

                throw;
            }
        }
    }
}
