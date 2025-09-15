using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MiniIAM.Infrastructure.Auth;
using MiniIAM.Infrastructure.Auth.Abstractions;
using MiniIAM.Infrastructure.Caching;
using MiniIAM.Infrastructure.Caching.Abstractions;
using MiniIAM.Infrastructure.Caching.Providers;
using MiniIAM.Infrastructure.Data.Repositories.Roles;
using MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions;
using MiniIAM.Infrastructure.Data.Repositories.Users;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;
using MinimalCqrs;

namespace MiniIAM.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddMinimalCqrs();
        builder.Services.AddMemoryCache();
        
        builder.Services.AddScoped<ICacheProvider, MemoryCacheProvider>();
        builder.Services.AddSingleton<ICachingService, CachingService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        
        builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
        builder.Services.AddScoped<IUserWriteRepository, UserWriteRepository>();
        builder.Services.AddScoped<IRoleReadRepository, RoleReadRepository>();
        builder.Services.AddScoped<IRoleWriteRepository, RoleWriteRepository>();
        
        return builder;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // builder.Services.AddScoped<IQueryHandler<GetMoviesQuery, List<Movie>>, GetMoviesQueryHandler>();
        // builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        return services;
    }
}