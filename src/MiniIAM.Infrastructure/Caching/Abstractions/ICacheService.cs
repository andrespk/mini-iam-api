namespace MiniIAM.Infrastructure.Caching.Abstractions;

public interface ICachingService
{
    public string ProviderName {get;}
    public Task SetAsync<T>(string key, T value, TimeSpan expiry);
    public Task<T?> GetAsync<T>(string key);
    public bool KeyExists(string key);
    public Task RemoveAsync(string key);
}