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