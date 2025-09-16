using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MiniIAM.Infrastructure.Cqrs.Dispatchers;
using Movies.Endpoints;

namespace Movies.Extensions
{
    public static partial class WebApplicationExtensions
    {
        public static IServiceCollection AddCqrs(this IServiceCollection services)
        {
            services.AddScoped<ICommandDispatcher, CommandDispatcher>();
            services.AddScoped<IQueryDispatcher, QueryDispatcher>();
            services.AddScoped<IHandlerContext, HandlerContext>();

            var appAssembly = typeof(MiniIAM.Application.UseCases.Auth.LogInUser).Assembly;
            foreach (var type in appAssembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface))
            {
                foreach (var itf in type.GetInterfaces())
                {
                    if (!itf.IsGenericType) continue;
                    var def = itf.GetGenericTypeDefinition();
                    var isHandler = def == typeof(ICommandHandler<>) ||
                                    def == typeof(ICommandHandler<,>) ||
                                    def == typeof(IQueryHandler<,>);
                    if (isHandler)
                    {
                        services.AddScoped(itf, type);
                    }
                }
            }

            return services;
        }

        public static WebApplication MapEndpoints(this WebApplication app)
        {
            AuthEndpoints.Map(app);
            UsersEndpoints.Map(app);
            return app;
        }
    }
}
