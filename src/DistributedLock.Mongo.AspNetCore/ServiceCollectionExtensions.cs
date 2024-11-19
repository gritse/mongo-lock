using DistributedLock.Mongo;
using DistributedLock.Mongo.AspNetCore;
using MongoDB.Bson;
using MongoDB.Driver;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务扩展类
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// inject service by <see cref="IMongoClient"/> from DI 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"><see cref="LockOptions"/></param>
    /// <typeparam name="T"> <see cref="Guid"/> | <see cref="ObjectId"/> </typeparam>
    public static void AddMongoDistributedLock<T>(this IServiceCollection services, Action<LockOptions>? options = null)
    {
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IMongoClient>();
        services.AddMongoDistributedLock<T>(client, options);
    }

    /// <summary>
    /// inject service by <see cref="IMongoClient"/>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="client"><see cref="IMongoClient"/></param>
    /// <param name="options"><see cref="LockOptions"/></param>
    /// <typeparam name="T"> <see cref="Guid"/> | <see cref="ObjectId"/> </typeparam>
    public static void AddMongoDistributedLock<T>(this IServiceCollection services, IMongoClient client, Action<LockOptions>? options = null)
    {
        var option = new LockOptions();
        options?.Invoke(option);
        var db = client.GetDatabase(option.DatabaseName);
        services.AddMongoDistributedLock<T>(db, options);
    }

    /// <summary>
    /// inject Service by <see cref="IMongoDatabase"/>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="db"><see cref="IMongoDatabase"/></param>
    /// <param name="options"><see cref="LockOptions"/></param>
    /// <typeparam name="T"> <see cref="Guid"/> | <see cref="ObjectId"/> </typeparam>
    public static void AddMongoDistributedLock<T>(this IServiceCollection services, IMongoDatabase db, Action<LockOptions>? options = null)
    {
        var option = new LockOptions();
        options?.Invoke(option);
        try
        {
            db.CreateCollection(option.SignalCollName, new()
            {
                MaxDocuments = option.MaxDocument,
                MaxSize = option.MaxSize,
                Capped = true
            });
        }
        catch
        {
            // ignored
        }
        var locks = db.GetCollection<LockAcquire<T>>(option.AcquireCollName);
        var signal = db.GetCollection<ReleaseSignal>(option.SignalCollName);
        services.AddSingleton(_ => MongoLockFactory<T>.Instance(locks, signal));
    }
}