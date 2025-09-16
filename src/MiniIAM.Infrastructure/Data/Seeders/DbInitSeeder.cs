using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Data.Contexts;

namespace MiniIAM.Infrastructure.Data.Seeders;

public class DbInitDataSeeder
{
    private readonly MainDbContext _context;

    public DbInitDataSeeder(MainDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seederUserId = Guid.Empty;
        var demoPassword = BCrypt.Net.BCrypt.HashPassword("Demo@321");

        if (!_context.Roles.Any())
        {
            _context.Roles.Add(new Role(Guid.NewGuid(), "Admin", null,
                new DataChangesHistory(DateTime.UtcNow, seederUserId)));
            _context.Roles.Add(new Role(Guid.NewGuid(), "Developer", null,
                new DataChangesHistory(DateTime.UtcNow, seederUserId)));

            await _context.SaveChangesAsync(ct);
        }

        var roles = _context.Roles.ToList();

        if (!_context.Users.Any())
        {
            _context.Users.AddRange(
                new User(Guid.NewGuid(), "Demo user", "demo@aviater.com", demoPassword,
                    roles.Where(x => x.Name == "Developer").ToList(),
                    new DataChangesHistory(DateTime.UtcNow, seederUserId))
            );

            await _context.SaveChangesAsync(ct);
        }
    }
}