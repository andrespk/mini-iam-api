namespace MiniIAM.Infrastructure.Caching.Abstractions;

public interface ICacheProvider
{
    public string Name { get; }
    public Task SetAsync<T>(string key, T value, TimeSpan expiry);
    public Task<T?> GetAsync<T>(string key);
    public bool KeyExists(string key);
}