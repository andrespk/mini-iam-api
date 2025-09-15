using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Shared.Security;

namespace MiniIAM.Shared.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Registers MiniIAM infrastructure (EF Core InMemory, Auth, Caching, CQRS/DI).
    /// </summary>
    public static WebApplicationBuilder AddMiniIamInfrastructure(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;

        // EF Core InMemory DB
        services.AddDbContext<MainDbContext>(options =>
        {
            options.UseInMemoryDatabase("MiniIamDb");
        });

        // Authentication/JWT
        var jwtKey = config.GetValue<string>("Jwt:Key") ?? "dev-secret-key-MiniIAM-ChangeMe";
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

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Application/Infrastructure registrations that already exist
        builder.AddInfrastructure(); // existing method from ServiceCollectionExtensions

        return builder;
    }

    /// <summary>
    /// Adds MiniIAM API endpoints (Auth, Users, Roles).
    /// </summary>
    public static WebApplication UseMiniIamApi(this WebApplication app)
    {
        // Map endpoints implemented in MiniIAM.Api
        MiniIAM.Api.Endpoints.AuthEndpoints.Map(app);
        MiniIAM.Api.Endpoints.UsersEndpoints.Map(app);
        MiniIAM.Api.Endpoints.RolesEndpoints.Map(app);
        return app;
    }
}
