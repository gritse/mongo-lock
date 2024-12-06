using MongoDB.Driver;

namespace DistributedLock.Mongo;

/// <summary>
///  Initializes a new instance of the MongoLock class with a locks and signals collections and lock identifier
/// </summary>
/// <param name="locks">MongoCollection to store locks, shouldn't be a capped collection</param>
/// <param name="signals">MongoCollection to push signals to release, should be a capped collection</param>
/// <param name="lock">Identifier of exclusive lock</param>
public sealed class MongoLock<T>(IMongoCollection<LockAcquire<T>> locks, IMongoCollection<ReleaseSignal> signals, T @lock) : IDistributedLock
{
    private readonly FilterDefinitionBuilder<LockAcquire<T>> _builder = new();
    private readonly IMongoCollection<LockAcquire<T>> _locks = locks ?? throw new ArgumentNullException(nameof(locks));
    private readonly IMongoCollection<ReleaseSignal> _signals = signals ?? throw new ArgumentNullException(nameof(signals));

    /// <inheritdoc />
    public async Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout)
    {
        if (lifetime < TimeSpan.Zero || lifetime > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(lifetime), "The value of lifetime in milliseconds is negative or is greater than MaxValue");
        if (timeout < TimeSpan.Zero || timeout > TimeSpan.MaxValue) throw new ArgumentOutOfRangeException(nameof(timeout), "The value of timeout in milliseconds is negative or is greater than MaxValue");

        var acquireId = Guid.NewGuid();

        while (await TryUpdate(lifetime, acquireId) == false)
        {
            using var cursor = await _locks.FindAsync(_builder.Eq(x => x.Id, @lock));
            var acquire = await cursor.FirstOrDefaultAsync();

            if (acquire != null && await WaitSignal(acquire.AcquireId, timeout) == false)
            {
                return await TryUpdate(lifetime, acquireId) == false ? new() : new AcquireResult(acquireId, this);
            }
        }

        return new AcquireResult(acquireId, this);
    }

    /// <summary>
    /// Generate new lock
    /// </summary>
    /// <param name="locks"></param>
    /// <param name="signals"></param>
    /// <param name="lockId"></param>
    /// <returns></returns>
    public static IDistributedLock GenerateNew(IMongoCollection<LockAcquire<T>> locks, IMongoCollection<ReleaseSignal> signals, T lockId) => new MongoLock<T>(locks, signals, lockId);

    /// <inheritdoc />
    public async Task ReleaseAsync(IAcquire acquire)
    {
        if (acquire.Acquired == false) return;

        var updateResult = await _locks.UpdateOneAsync(
            filter: _builder.And(_builder.Eq(x => x.Id, @lock), _builder.Eq(x => x.AcquireId, acquire.AcquireId)), // x => x.Id == _id && x.AcquireId == acquire.AcquireId,
            update: new UpdateDefinitionBuilder<LockAcquire<T>>().Set(x => x.Acquired, false));

        if (updateResult.IsAcknowledged && updateResult.ModifiedCount > 0)
            await _signals.InsertOneAsync(new() { AcquireId = acquire.AcquireId });
    }

    private async Task<bool> WaitSignal(Guid acquireId, TimeSpan timeout)
    {
        using var cursor = await _signals.Find(x => x.AcquireId == acquireId, new() { MaxAwaitTime = timeout, CursorType = CursorType.TailableAwait }).ToCursorAsync();
        var started = DateTime.UtcNow;

        while (await cursor.MoveNextAsync())
        {
            if (cursor.Current.Any()) return true;
            if (DateTime.UtcNow - started >= timeout) return false;
        }

        return false;
    }

    private async Task<bool> TryUpdate(TimeSpan lifetime, Guid acquireId)
    {
        try
        {
            var update = new UpdateDefinitionBuilder<LockAcquire<T>>()
                .Set(x => x.Acquired, true)
                .Set(x => x.ExpiresIn, DateTime.UtcNow + lifetime)
                .Set(x => x.AcquireId, acquireId)
                .SetOnInsert(x => x.Id, @lock);

            var filter = _builder.And(
                _builder.Eq(x => x.Id, @lock),
                _builder.Or(
                    _builder.Eq(x => x.Acquired, false),
                    _builder.Lte(x => x.ExpiresIn, DateTime.UtcNow)
                )
            );

            var updateResult = await _locks.UpdateOneAsync(
                filter: filter, // x => x.Id == _id && (!x.Acquired || x.ExpiresIn <= DateTime.UtcNow),
                update: update, options: new() { IsUpsert = true });

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