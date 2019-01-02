# mongo-lock
Exclusive distributed locking for .NET and MongoDB

## Usage

```c#
const string сonnectionString = "--mognodb connection string--";

MongoClient client = new MongoClient(сonnectionString);
IMongoDatabase database = client.GetDatabase("sample");

// Regular collection, not a capped!
IMongoCollection<LockAcquire<Guid>> locks = database.GetCollection<LockAcquire<Guid>>("locks");

// This collection should be a capped! https://docs.mongodb.com/manual/core/capped-collections/
// The size of the cappred collection should be enough to put all active locks.
// One ReleaseSignal is about 32 bytes, so for 100,000 simultaneously locks,
// you need a capped collection size ~3 megabytes
IMongoCollection<ReleaseSignal> signals = database.GetCollection<ReleaseSignal>("signals");

Guid lockId = Guid.Parse("BF431614-4FB0-4489-84AA-D3EFEEF6BE7E");
// Guid lockId is a name of your distributed lock. You can also use string, int, etc.
MongoLock<Guid> mongoLock = new MongoLock<Guid>(locks, signals, lockId);

// Try to acquire exclusve lock. Lifetime for created lock is 30 secounds
IAcquire acquire = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));

try
{
    if (acquire.Acquired)
    {
        // critical section, it cannot be executed by more than one thread on any server at a time
        // ...
        // ...
    }
    else
    {
        // Timeout! Maybe another thread did not release the lock... We can try again or throw excepton
    }
}
finally
{
    // if (acquire.Acquired) no need to do it manually
    await mongoLock.ReleaseAsync(acquire);
}
```

## How it works?
- The lock is acquiring by the `lockId`. It can be `Guid`, `string`, `int` or any supported by MongoDB.Driver type.
- When you try to acquire the lock, the document with specifed lockId is added to the `locks` collection OR updates if it exists.
- When the lock is released, the document is updated and a new document is added to the `signals` capped collection
- When the lock is awaiting, the tailable cursor with server-side awaiting is used. Details: https://docs.mongodb.com/manual/reference/method/cursor.tailable/
- `Lifetime` is a period of time during which the lock is valid. After this time, the lock is "released" automatically and can be acquired again. It prevents deadlocks.
- **Do not use a long `timeout`, this can throw exception by MongoDB Driver. A normal timeout is no more than 1-2 minutes!**
