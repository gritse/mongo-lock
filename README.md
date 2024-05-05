# DistributedLock.Mongo: Exclusive Distributed Locking for .NET

[![NuGet version (DistributedLock.Mongo)](https://img.shields.io/nuget/v/DistributedLock.Mongo.svg?style=flat-square)](https://www.nuget.org/packages/DistributedLock.Mongo/)

DistributedLock.Mongo is a class library available on NuGet that enables exclusive distributed locking mechanisms for applications using .NET and MongoDB. It facilitates safe coordination of operations across different system components by managing access to shared resources. For more details or to download the library, visit the NuGet [page](https://www.nuget.org/packages/DistributedLock.Mongo/).

## Usage

```c#
const string connectionString = ""; // mognodb connection string

MongoClient client = new MongoClient(connectionString);
IMongoDatabase database = client.GetDatabase("sample-db");

// Regular collection, not a capped!
IMongoCollection<LockAcquire<Guid>> locks = database.GetCollection<LockAcquire<Guid>>("locks");

// Ensure this collection is capped. Refer to MongoDB's documentation on capped collections:
// https://docs.mongodb.com/manual/core/capped-collections/
// The capped collection size should be sufficient to accommodate all active locks. With each ReleaseSignal estimated at 32 bytes,
// approximately 3 megabytes will be required to handle up to 100,000 simultaneous locks.
IMongoCollection<ReleaseSignal> signals = database.GetCollection<ReleaseSignal>("signals");

// The 'lockId' variable acts as the identifier for your distributed lock. It can be a Guid, string, int, or any other suitable type
// depending on your application's requirements and the characteristics of the locks being implemented.
Guid lockId = Guid.Parse("BF431614-4FB0-4489-84AA-D3EFEEF6BE7E");
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
