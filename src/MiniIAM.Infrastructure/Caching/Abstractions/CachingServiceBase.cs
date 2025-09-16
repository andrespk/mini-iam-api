namespace MiniIAM.Infrastructure.Caching.Abstractions;

public abstract class CachingServiceBase(ICacheProvider provider) : ICachingService
{
    public string ProviderName => provider.Name;
    public Task SetAsync<T>(string key, T value, TimeSpan expiry) => provider.SetAsync<T>(key, value, expiry);
    
    public Task<T?> GetAsync<T>(string key) => provider.GetAsync<T>(key);

    public bool KeyExists(string key) => provider.KeyExists(key);
    
    public Task RemoveAsync(string key) => provider.RemoveAsync(key);
}