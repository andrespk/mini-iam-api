using MiniIAM.Infrastructure.Caching.Abstractions;

namespace MiniIAM.Infrastructure.Caching;

public class CachingService(ICacheProvider provider) : CachingServiceBase(provider)
{
    
}