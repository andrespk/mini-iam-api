using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MiniIAM.Infrastructure.Caching.Abstractions;
using StackExchange.Redis;

namespace MiniIAM.Infrastructure.Caching.Providers;

public class RedisCacheProvider : ICacheProvider
{
    private readonly IDatabase _db;

    public string Name => nameof(RedisCacheProvider);

    public RedisCacheProvider(IConfiguration config)
    {
        var redis = ConnectionMultiplexer.Connect(config.GetConnectionString("Redis") ?? string.Empty);
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public bool KeyExists(string key) => _db.KeyExists(new RedisKey(key));
}