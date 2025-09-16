using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Domain.Users.Dtos;
using System;
using System.Linq;

namespace MiniIAM.Tests.Integration;

public class CustomApiFactory : WebApplicationFactory<MiniIAM.Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Ensure database is created and seeded for tests
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Database.EnsureCreated();

            if (!db.Users.Any())
            {
                var admin = new User(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Admin", "admin@local",
                    BCrypt.Net.BCrypt.HashPassword("admin"), roles: new List<MiniIAM.Domain.Roles.Entities.Role>(), 
                    changesHistory: new MiniIAM.Domain.Abstractions.DataChangesHistory());
                db.Add(admin);
                db.SaveChanges();
            }
        });

        return base.CreateHost(builder);
    }
}
