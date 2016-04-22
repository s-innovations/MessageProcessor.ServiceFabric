using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Common.Logging;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Stores
{

    ///// <summary>
    ///// Extension methods for <see cref="IdentityServer3.Core.Services.ICache{T}"/>
    ///// </summary>
    //public static class ICacheExtensions
    //{
    //    private static readonly ILog Logger = LogProvider.GetLogger("Cache");

    //    /// <summary>
    //    /// Attempts to get an item from the cache. If the item is not found, the <c>get</c> function is used to 
    //    /// obtain the item and populate the cache.
    //    /// </summary>
    //    /// <typeparam name="T"></typeparam>
    //    /// <param name="cache">The cache.</param>
    //    /// <param name="key">The key.</param>
    //    /// <param name="get">The get function.</param>
    //    /// <returns></returns>
    //    /// <exception cref="System.ArgumentNullException">
    //    /// cache
    //    /// or
    //    /// get
    //    /// </exception>
    //    public static async Task<T> GetAsync<T>(this ICache<T> cache, string key, Func<Task<T>> get)
    //        where T : class
    //    {
    //        if (cache == null) throw new ArgumentNullException("cache");
    //        if (get == null) throw new ArgumentNullException("get");
    //        if (key == null) return null;

    //        T item = await cache.GetAsync(key);

    //        if (item == null)
    //        {
    //            Logger.Debug("Cache miss: " + key);

    //            item = await get();

    //            if (item != null)
    //            {
    //                await cache.SetAsync(key, item);
    //            }
    //        }
    //        else
    //        {
    //            Logger.Debug("Cache hit: " + key);
    //        }

    //        return item;
    //    }
    //}

    ///// <summary>
    ///// Abstract interface to model data caching
    ///// </summary>
    ///// <typeparam name="T">The data type to be cached</typeparam>
    //public interface ICache<T>
    //    where T : class
    //{
    //    /// <summary>
    //    /// Gets the cached data based upon a key index.
    //    /// </summary>
    //    /// <param name="key">The key.</param>
    //    /// <returns>The cached item, or <c>null</c> if no item matches the key.</returns>
    //    Task<T> GetAsync(string key);

    //    /// <summary>
    //    /// Caches the data based upon a key
    //    /// </summary>
    //    /// <param name="key">The key.</param>
    //    /// <param name="item">The item.</param>
    //    /// <returns></returns>
    //    Task SetAsync(string key, T item);
    //}
    ///// <summary>
    ///// In-memory, time based implementation of <see cref="ICache{T}"/>
    ///// </summary>
    ///// <typeparam name="T"></typeparam>
    //public class DefaultCache<T> : ICache<T>
    //    where T : class
    //{
    //    readonly MemoryCache cache;
    //    readonly TimeSpan duration;

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="DefaultCache{T}"/> class.
    //    /// </summary>
    //    public DefaultCache()
    //        : this(TimeSpan.FromMinutes(5))
    //    {
    //    }

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="DefaultCache{T}"/> class.
    //    /// </summary>
    //    /// <param name="duration">The duration to cache items.</param>
    //    /// <exception cref="System.ArgumentOutOfRangeException">duration;Duration must be greater than zero</exception>
    //    public DefaultCache(TimeSpan duration)
    //    {
    //        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("duration", "Duration must be greater than zero");

    //        this.cache = new MemoryCache("cache");
    //        this.duration = duration;
          
    //    }

    //    /// <summary>
    //    /// Gets the cached data based upon a key index.
    //    /// </summary>
    //    /// <param name="key">The key.</param>
    //    /// <returns>
    //    /// The cached item, or <c>null</c> if no item matches the key.
    //    /// </returns>
    //    public Task<T> GetAsync(string key)
    //    {
    //        return Task.FromResult((T)cache.Get(key));
    //    }

    //    /// <summary>
    //    /// Caches the data based upon a key
    //    /// </summary>
    //    /// <param name="key">The key.</param>
    //    /// <param name="item">The item.</param>
    //    /// <returns></returns>
    //    public Task SetAsync(string key, T item)
    //    {
    //        var expiration = UtcNow.Add(this.duration);
    //        cache.Set(key, item, expiration);
    //        return Task.FromResult(0);
    //    }

    //    /// <summary>
    //    /// Gets the UTC now.
    //    /// </summary>
    //    /// <value>
    //    /// The UTC now.
    //    /// </value>
    //    protected virtual DateTimeOffset UtcNow
    //    {
    //        get { return DateTimeOffset.UtcNow; }
    //    }
    //}
    //public class ClusterExistResult
    //{
       
    //    public ClusterExistResult(bool exists)
    //    {
    //        this.Exists = exists;
    //    }

    //    public bool Exists { get; set; }
    //}
    //public interface IStatelessCachedClusterStoreService : IService
    //{
    //    Task ClearStateAsync(string clusterKey);
    //}
    //public class StatelessCachedClusterCacheService: StatelessService, IStatelessCachedClusterStoreService
    //{
       
    //    public StatelessCachedClusterCacheService(StatelessServiceContext state) : base(state) { 
        

    //    }

    //    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    //    {
    //        return new[] { new ServiceInstanceListener(context =>
    //            this.CreateServiceRemotingListener(context),this.Context.NodeContext.NodeInstanceId.ToString()) };
    //    }

       
    //    public Task ClearStateAsync(string clusterKey)
    //    {
    //        return Task.FromResult(0);
    //    }
    //}
    //public class StatelessCachedClusterStore :  IMessageClusterConfigurationStore
    //{
    //    private readonly IMessageClusterConfigurationStore _inner;
    //    private readonly DefaultCache<ClusterExistResult> _messageClusterResourceExistsCache;
    //    private readonly DefaultCache<MessageClusterResource> _messageClusterResourceCache;
    //    public StatelessCachedClusterStore(IMessageClusterConfigurationStore inner)
    //    {
    //        this._inner = inner;
    //        this._messageClusterResourceExistsCache = new DefaultCache<ClusterExistResult>();
    //        this._messageClusterResourceCache = new DefaultCache<MessageClusterResource>();

    //    }



    //    public async Task<bool> ClusterExistsAsync(string clusterKey)
    //    {
    //        var  cluster = await this._messageClusterResourceExistsCache.GetAsync(clusterKey,  async () =>  new ClusterExistResult(await _inner.ClusterExistsAsync(clusterKey)));
    //        return cluster.Exists;
    //    }

    //    public Task<MessageClusterResource> GetMessageClusterAsync(string clusterKey)
    //    {
    //        return this._messageClusterResourceCache.GetAsync(clusterKey, () => this._inner.GetMessageClusterAsync(clusterKey));
    //    }

    //    public async Task<MessageClusterResourceBase> GetMessageClusterResourceAsync(string clusterKey)
    //    {
    //        var cluster = await GetMessageClusterAsync(clusterKey.Substring(0, clusterKey.LastIndexOf('/')));
    //        var name = clusterKey.Substring(clusterKey.LastIndexOf('/') + 1);
    //        return cluster.Resources.FirstOrDefault(n => n.Name == name);
    //    }

    //    public async Task<MessageClusterResource> PutMessageClusterAsync(string clusterKey, MessageClusterResource model)
    //    {           
    //        var updated= await _inner.PutMessageClusterAsync(clusterKey, model);

    //        var services = ServiceProxy.Create<IStatelessCachedClusterStoreService>(new Uri("fabric:/MessageProcessor.ServiceFabricHostType/StatelessCachedClusterCacheService"));

    //        await services.ClearStateAsync(clusterKey);

    //        return updated;
    //    }

      
    //}
}
