using Microsoft.EntityFrameworkCore;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Domain.Users.Entitties;

namespace MiniIAM.Infrastructure.Data.Contexts;

public class MainDbContext : DbContext
{
    public DbSet<User> Users { get; }
    public DbSet<Role> Roles { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id); // Primary key
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(254);
            entity.Property(x => x.Password)
                .IsRequired()
                .HasMaxLength(254);
            modelBuilder.Entity<User>()
                .HasMany(x => x.Roles)
                .WithMany(x => x.Users)
                .UsingEntity(x => x.ToTable("UserRoles"));
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
            modelBuilder.Entity<User>().HasQueryFilter(b => !b.IsDeleted);
        });
        
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);
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
            modelBuilder.Entity<Role>().HasQueryFilter(b => !b.IsDeleted);
        });
    }
}