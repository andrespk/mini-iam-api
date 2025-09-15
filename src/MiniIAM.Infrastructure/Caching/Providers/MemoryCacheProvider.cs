using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MiniIAM.Infrastructure.Caching.Abstractions;

namespace MiniIAM.Infrastructure.Caching.Providers;

public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;

    public string Name => nameof(MemoryCacheProvider);

    public MemoryCacheProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return Task.FromResult(value is string json
                ? JsonSerializer.Deserialize<T>(json)
                : (T?)value);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        var json = JsonSerializer.Serialize(value);
        _cache.Set(key, json, expiry);
        return Task.CompletedTask;
    }

    public bool KeyExists(string key)
    {
        return _cache.TryGetValue(key, out _);
    }
}