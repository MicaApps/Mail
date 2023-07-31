using System;
using System.Collections.Generic;

namespace Mail.Services;

public class CacheService : ICacheService
{
    private readonly Dictionary<Type, object> Caches = new();

    public void AddOrReplaceCache<T>(T cache) where T : class
    {
        Caches[typeof(T)] = cache;
    }

    public void RemoveCache<T>()
    {
        Caches.Remove(typeof(T));
    }

    public T? GetCache<T>() where T : class
    {
        var val = Caches!.GetValueOrDefault(typeof(T), null);
        if (val is T valT)
        {
            return valT;
        }

        return null;
    }
}