using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MiniIAM.Api.Endpoints;
using MiniIAM.Application.UseCases.Auth;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MiniIAM.Infrastructure.Cqrs.Dispatchers;
using Movies.Endpoints;
using Movies.Swagger;

namespace MiniIAM.Shared.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Adds core infrastructure services: CQRS dispatchers/handlers, caching, auth, repositories.
    /// </summary>
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        // MinimalCqrs dispatchers
        builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        
        
        // Caching
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<Infrastructure.Caching.Abstractions.ICachingService, Infrastructure.Caching.CachingService>();
        builder.Services.AddScoped<Infrastructure.Caching.Abstractions.ICacheProvider, Infrastructure.Caching.Providers.MemoryCacheProvider>();

        // Auth
        builder.Services.AddScoped<Infrastructure.Auth.Abstractions.IAuthService, Infrastructure.Auth.AuthService>();

        // Repositories
        builder.Services.AddScoped<Infrastructure.Data.Repositories.Users.Abstractions.IUserReadRepository, Infrastructure.Data.Repositories.Users.UserReadRepository>();
        builder.Services.AddScoped<Infrastructure.Data.Repositories.Users.Abstractions.IUserWriteRepository, Infrastructure.Data.Repositories.Users.UserWriteRepository>();
        builder.Services.AddScoped<Infrastructure.Data.Repositories.Roles.Abstractions.IRoleReadRepository, Infrastructure.Data.Repositories.Roles.RoleReadRepository>();
        builder.Services.AddScoped<Infrastructure.Data.Repositories.Roles.Abstractions.IRoleWriteRepository, Infrastructure.Data.Repositories.Roles.RoleWriteRepository>();
        
        var appAssembly = System.Reflection.Assembly.GetAssembly(typeof(LogInUser));
        if (appAssembly != null)
        {
            foreach (var type in appAssembly.GetTypes())
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType)
                    {
                        var gen = iface.GetGenericTypeDefinition();
                        if (gen == typeof(ICommandHandler<,>))
                            builder.Services.AddScoped(iface, type);
                        else if (gen == typeof(IQueryHandler<,>))
                            builder.Services.AddScoped(iface, type);
                    }
                }
            }
        }

        return builder;
    }

    public static WebApplicationBuilder AddMiniIamInfrastructure(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;

        // EF Core InMemory
        services.AddDbContext<MainDbContext>(options =>
        {
            options.UseInMemoryDatabase("MainDb");
        });

        // API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                new Asp.Versioning.UrlSegmentApiVersionReader(),
                new Asp.Versioning.HeaderApiVersionReader("x-api-version"),
                new Asp.Versioning.MediaTypeApiVersionReader("x-api-version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // JWT Auth
        var jwtKey = config.GetValue<string>("Jwt:Key") ?? "dev-secret-key-change-me";
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateLifetime = true
                };
            });
        services.AddAuthorization();

        // Swagger (OpenAPI) enriched
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "MiniIAM API",
                Version = "v1",
                Description = "Identity & Access Minimal API following CQRS and clean layering. JWT-protected endpoints.",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact { Name = "MiniIAM", Email = "support@example.com" },
                License = new Microsoft.OpenApi.Models.OpenApiLicense { Name = "MIT" }
            });

            // Aplicar o OperationFilter apenas uma vez, no contexto correto do Swagger
            options.OperationFilter<DefaultResponsesOperationFilter>();

            // JWT Bearer
            var scheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            };
            options.AddSecurityDefinition("Bearer", scheme);
            
            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference 
                        { 
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, 
                            Id = "Bearer" 
                        }
                    },
                    new string[] { }
                }
            });

            var xml = Path.Combine(AppContext.BaseDirectory, "MiniIAM.Api.xml");
            if (File.Exists(xml)) 
                options.IncludeXmlComments(xml, includeControllerXmlComments: true);
        });

        builder.AddInfrastructure();

        return builder;
    }

    public static WebApplication UseMiniIamApi(this WebApplication app)
    {
        AuthEndpoints.Map(app);
        UsersEndpoints.Map(app);
        RolesEndpoints.Map(app);
        return app;
    }
}