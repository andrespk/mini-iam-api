using Microsoft.EntityFrameworkCore;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Domain.Sessions.Entities;
using MiniIAM.Domain.Users.Entitties;

namespace MiniIAM.Infrastructure.Data.Contexts;

public class MainDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;
    
    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(254);
            entity.Property(x => x.Password)
                .IsRequired()
                .HasMaxLength(254);
            entity.OwnsOne(x => x.ChangesHistory);
            
            var rolesNav = entity.Metadata.FindNavigation(nameof(User.Roles));
            if (rolesNav != null)
            {
                rolesNav.SetField("_roles");
                rolesNav.SetPropertyAccessMode(PropertyAccessMode.Field);
            }
            
            modelBuilder.Entity<User>()
                .HasMany(x => x.Roles)
                .WithMany(x => x.Users)
                .UsingEntity("UsersVsRoles");
            modelBuilder.Entity<User>()
                .OwnsOne(c => c.ChangesHistory, ch =>
                {
                    ch.Property(a => a.CreatedAtUtc).HasColumnName("CreatedAtUtc");
                    ch.Property(a => a.CreatedByUserId).HasColumnName("CreatedByUserId");
                    ch.Property(a => a.UpdatedAtUtc).HasColumnName("UpdatedAtUtc");
                    ch.Property(a => a.UpdatedByUserId).HasColumnName("UpdatedByUserId");
                    ch.Property(a => a.DeletedAtUtc).HasColumnName("DeletedAtUtc");
                    ch.Property(a => a.DeletedByUserId).HasColumnName("DeletedByUserId");
                });
            // modelBuilder.Entity<User>().HasQueryFilter(b => !b.IsDeleted);
        });
        
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);
            var usersNav = entity.Metadata.FindNavigation(nameof(Role.Users));
            if (usersNav != null)
            {
                usersNav.SetField("_users");
                usersNav.SetPropertyAccessMode(PropertyAccessMode.Field);
            }
            entity.OwnsOne(x => x.ChangesHistory);
            modelBuilder.Entity<Role>()
                .HasMany(x => x.Users)
                .WithMany(x => x.Roles)
                .UsingEntity(x => x.ToTable("UserRoles"));
            modelBuilder.Entity<Role>()
                .OwnsOne(c => c.ChangesHistory, ch =>
                {
                    ch.Property(a => a.CreatedAtUtc).HasColumnName("CreatedAtUtc");
                    ch.Property(a => a.CreatedByUserId).HasColumnName("CreatedByUserId");
                    ch.Property(a => a.UpdatedAtUtc).HasColumnName("UpdatedAtUtc");
                    ch.Property(a => a.UpdatedByUserId).HasColumnName("UpdatedByUserId");
                    ch.Property(a => a.DeletedAtUtc).HasColumnName("DeletedAtUtc");
                    ch.Property(a => a.DeletedByUserId).HasColumnName("DeletedByUserId");
                });
            // modelBuilder.Entity<Role>().HasQueryFilter(b => !b.IsDeleted);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.LastRefreshedAtUtc);
            entity.HasIndex(x => x.AccessToken).IsUnique();
            
            entity.Property(x => x.AccessToken)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(x => x.RefreshToken)
                .IsRequired()
                .HasMaxLength(500);
            
            // Configure ChangesHistory as owned entity
            entity.OwnsOne(x => x.ChangesHistory, ch =>
            {
                ch.Property(a => a.CreatedAtUtc).HasColumnName("CreatedAtUtc");
                ch.Property(a => a.CreatedByUserId).HasColumnName("CreatedByUserId");
                ch.Property(a => a.UpdatedAtUtc).HasColumnName("UpdatedAtUtc");
                ch.Property(a => a.UpdatedByUserId).HasColumnName("UpdatedByUserId");
                ch.Property(a => a.DeletedAtUtc).HasColumnName("DeletedAtUtc");
                ch.Property(a => a.DeletedByUserId).HasColumnName("DeletedByUserId");
            });
        });
    }
}