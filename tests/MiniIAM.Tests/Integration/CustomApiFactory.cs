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
            // Ensure database is created for tests
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Database.EnsureCreated();
        });

        return base.CreateHost(builder);
    }
}
