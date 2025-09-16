using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Data.Seeders;
using MiniIAM.Swagger;

namespace MiniIAM.Extensions;

public static partial class WebApplicationExtensions
{
    public static WebApplicationBuilder InitInfrastructure(this WebApplicationBuilder builder)
    {
        // Caching
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<MiniIAM.Infrastructure.Caching.Abstractions.ICachingService, MiniIAM.Infrastructure.Caching.CachingService>();
        builder.Services.AddScoped<MiniIAM.Infrastructure.Caching.Abstractions.ICacheProvider, MiniIAM.Infrastructure.Caching.Providers.MemoryCacheProvider>();

        // Auth
        builder.Services.AddScoped<MiniIAM.Infrastructure.Auth.Abstractions.IAuthService, MiniIAM.Infrastructure.Auth.AuthService>();

        // Repositories
        builder.Services.AddScoped<MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions.IUserReadRepository, MiniIAM.Infrastructure.Data.Repositories.Users.UserReadRepository>();
        builder.Services.AddScoped<MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions.IUserWriteRepository, MiniIAM.Infrastructure.Data.Repositories.Users.UserWriteRepository>();
        builder.Services.AddScoped<MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions.IRoleReadRepository, MiniIAM.Infrastructure.Data.Repositories.Roles.RoleReadRepository>();
        builder.Services.AddScoped<MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions.IRoleWriteRepository, MiniIAM.Infrastructure.Data.Repositories.Roles.RoleWriteRepository>();
        
        // CQRS
        builder.Services.AddCqrs();
        
        //Database
        builder.Services.AddDbContext<MainDbContext>(options =>
        {
            var dbName = builder.Configuration["Database:InMemory:Name"];

            if (string.IsNullOrEmpty(dbName))
                throw new NoNullAllowedException("Missing DB configuration.");
            
            options.UseInMemoryDatabase(dbName);
            
            if (builder.Environment.IsDevelopment())
                options.EnableSensitiveDataLogging();
        });
        
        //Initial Data Seeder
        builder.Services.AddScoped<DbInitDataSeeder>();

        return builder;
    }

    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
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

        builder.InitInfrastructure();

        return builder;
    }
}