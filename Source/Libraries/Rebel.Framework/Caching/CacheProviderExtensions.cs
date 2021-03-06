namespace Rebel.Framework.Caching
{
    using System;

    public static class CacheProviderExtensions
    {
        public static CacheCreationResult<T> GetOrCreate<T>(this AbstractCacheProvider provider, CacheKey key, Func<T> callback)
        {
            var cachePolicy = provider.GetCachePolicyForKey(key);
            return provider.GetOrCreate(key, () => new CacheValueOf<T>(callback.Invoke(), cachePolicy));
        }

        public static CacheCreationResult<T> GetOrCreate<T>(this AbstractCacheProvider provider, string key, Func<T> callback, ICachePolicy cachePolicy)
        {
            return provider.GetOrCreate(key, () => new CacheValueOf<T>(callback.Invoke(), cachePolicy));
        }
    }
}