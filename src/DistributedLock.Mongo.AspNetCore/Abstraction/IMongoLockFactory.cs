namespace DistributedLock.Mongo.AspNetCore.Abstraction;

/// <summary>
/// 工厂接口
/// </summary>
public interface IMongoLockFactory<in T>
{
    /// <summary>
    /// 创建客户端
    /// </summary>
    /// <param name="locks"></param>
    /// <returns></returns>
    IDistributedLock GenerateNewLock(T locks);
}