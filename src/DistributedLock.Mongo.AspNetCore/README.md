### DistributedLock.Mongo.AspNetCore

#### How to use

```csharp
// use default
builder.Services.AddMongoDistributedLock<Guid>();

// or use options
builder.Services.AddMongoDistributedLock<Guid>(op =>
{
    op.DatabaseName = "test_locks";
    op.MaxDocument = 100;
    ...
});
```

- use `IMongoLockFactory` from DI

```csharp
public class DistributedLockTest(IMongoLockFactory<Guid> lockFactory)
{
    public async Task<dynamic> AcquireLock()
    {
        var lockId = Guid.Parse("BF431614-4FB0-4489-84AA-D3EFEEF6BE7E");
        var locker = lockFactory.GenerateNewLock(lockId);

        var acq = await locker.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));
        try
        {
            // if (anotherAcquire.Acquired)
            // { ... }
        }
        finally
        {
            // if (acquire.Acquired) no need to do it manually
            await locker.ReleaseAsync(anotherAcquire);
        }
    }
}
```
