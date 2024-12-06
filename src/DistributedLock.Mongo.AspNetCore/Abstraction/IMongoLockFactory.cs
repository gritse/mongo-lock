namespace DistributedLock.Mongo.AspNetCore.Abstraction;

/// <summary>
/// lock factory
/// </summary>
public interface IMongoLockFactory<in T>
{
    /// <summary>
    /// generate new lock
    /// </summary>
    /// <param name="locks"></param>
    /// <returns></returns>
    IDistributedLock GenerateNewLock(T locks);
}