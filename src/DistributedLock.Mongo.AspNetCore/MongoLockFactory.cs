using DistributedLock.Mongo.AspNetCore.Abstraction;
using MongoDB.Driver;

namespace DistributedLock.Mongo.AspNetCore;

/// <inheritdoc />
public sealed class MongoLockFactory<T> : IMongoLockFactory<T>
{
    private MongoLockFactory(IMongoCollection<LockAcquire<T>> locks, IMongoCollection<ReleaseSignal> signal)
    {
        Locks = locks;
        Signal = signal;
    }

    private IMongoCollection<LockAcquire<T>> Locks { get; }

    private IMongoCollection<ReleaseSignal> Signal { get; }

    /// <inheritdoc />
    public IDistributedLock GenerateNewLock(T locks) => MongoLock<T>.GenerateNew(Locks, Signal, locks);

    internal static IMongoLockFactory<T> Instance(IMongoCollection<LockAcquire<T>> locks, IMongoCollection<ReleaseSignal> signal) => new MongoLockFactory<T>(locks, signal);
}