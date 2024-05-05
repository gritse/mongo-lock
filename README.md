# DistributedLock.Mongo: Exclusive Distributed Locking for .NET

[![NuGet version (DistributedLock.Mongo)](https://img.shields.io/nuget/v/DistributedLock.Mongo.svg?style=flat-square)](https://www.nuget.org/packages/DistributedLock.Mongo/)

DistributedLock.Mongo is a class library available on NuGet that enables exclusive distributed locking mechanisms for applications using .NET and MongoDB. It facilitates safe coordination of operations across different system components by managing access to shared resources. For more details or to download the library, visit the NuGet [page](https://www.nuget.org/packages/DistributedLock.Mongo/).

## Usage

```c#
using DistributedLock.Mongo;
using MongoDB.Driver;
using System;

const string connectionString = "mongodb://localhost:27017/"; // MongoDB connection string

var client = new MongoClient(connectionString);
var database = client.GetDatabase("sample-db");

// This is a regular collection, not a capped one.
var locks = database.GetCollection<LockAcquire<Guid>>("locks");

// Ensure this collection is capped. Refer to MongoDB's documentation on capped collections:
// https://docs.mongodb.com/manual/core/capped-collections/
// The capped collection size should be sufficient to accommodate all active locks. Each ReleaseSignal is estimated at 32 bytes,
// so approximately 3 megabytes will be required to handle up to 100,000 simultaneous locks.
var signals = database.GetCollection<ReleaseSignal>("signals");

// The 'lockId' variable serves as the identifier for your distributed lock. It can be a Guid, string, int, or any other suitable type
// depending on your application's requirements and the characteristics of the locks being implemented.
var lockId = Guid.Parse("BF431614-4FB0-4489-84AA-D3EFEEF6BE7E");
var mongoLock = new MongoLock<Guid>(locks, signals, lockId);

// Attempt to acquire an exclusive lock. The lifetime for the created lock is 30 seconds.
await using (var acquire = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10)))
{
    if (acquire.Acquired)
    {
        // Critical section: This block cannot be executed by more than one thread on any server at a time.
        // ...
        // ...
    }
    else
    {
        // Timeout occurred! It's possible another thread has not released the lock yet.
        // Consider retrying or handling this scenario with an exception.
    }
}

// Alternatively, you can handle the lock using a try...finally block as shown below:
var anotherAcquire = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));
try
{
    // if (anotherAcquire.Acquired)
    // { ... }
}
finally
{
    // if (acquire.Acquired) no need to do it manually
    await mongoLock.ReleaseAsync(anotherAcquire);
}
```

## How the Distributed Lock Works

- **Lock Identification**: The lock is identified by `lockId`, which can be of types such as `Guid`, `string`, `int`, or any other type supported by the MongoDB.Driver.
- **Acquisition**: When attempting to acquire the lock, a document corresponding to the specified `lockId` is either added to the `locks` collection or updated if it already exists.
- **Release**: Upon releasing the lock, the existing document in the `locks` collection is updated, and a new document is added to the `signals` collection, which is capped.
- **Lock Waiting**: While waiting for the lock, a tailable cursor is used for server-side waiting. For more details, see [MongoDB's tailable cursor documentation](https://docs.mongodb.com/manual/reference/method/cursor.tailable/).
- **Lifetime**: The lifetime of the lock specifies the duration for which the lock remains valid. After this period, the lock is automatically considered "released" and becomes available for acquisition again. This feature helps prevent deadlocks.
- **Timeout Caution**: Avoid setting a long `timeout` as it can lead to exceptions thrown by the MongoDB Driver. Typically, a timeout should not exceed 1-2 minutes.

