using Microsoft.EntityFrameworkCore;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Users.Dtos;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Users;

public class UserWriteRepository(MainDbContext context) : IUserWriteRepository
{
    public async Task<Result> InsertAsync(User entity, Guid byUserId, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            entity.SetInsertChangeHistory(byUserId);
            context.Users.Add(entity);
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> UpdateAsync(User entity, Guid byUserId, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var existingUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.Id);

            if (existingUser is null) return Result.Failure("User ID not found.", entity.Id);

            existingUser.Name = entity.Name;
            existingUser.Roles = entity.Roles;
            existingUser.SetUpdateChangeHistory(byUserId);
            context.Users.Update(existingUser);
            await context.SaveChangesAsync(ct);

            return Result<UserDto>.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var entity = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

            if (entity is null) return Result.NoDataFound();

            entity.DeleteChangeHistory(byUserId);
            context.Users.Update(entity);
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> SetPasswordAsync(Guid userId, string password, Guid byUserId,
        CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var user = context.Users.AsNoTracking().FirstOrDefault(x => x.Id == userId);
            if (user != null)
            {
                var encryptedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                user.Password = encryptedPassword;
                user.SetUpdateChangeHistory(byUserId);
                await context.SaveChangesAsync(ct);

                return Result.Success();
            }

            return Result.Failure("User ID not found.", userId);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> AddRolesAsync(Guid userId, IList<RoleDto> roles, Guid byUserId,
        CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var user = context.Users
                .AsNoTracking()
                .Include(user => user.Roles)
                .FirstOrDefault(x => x.Id == userId);
            if (user != null)
            {
                foreach (var role in user.Roles)
                {
                    if (roles.Any(x => x.Id == role.Id))
                    {
                        roles = roles.Where(x => x.Id != role.Id).ToList();
                    }
                }

                ;

                foreach (var role in roles.Select(x => x.ToEntity()))
                {
                    role.ChangesHistory.SetInsertChangesHistory(byUserId);
                    user.Roles.Add(role);
                }

                user.SetUpdateChangeHistory(byUserId);
                context.Users.Update(user);
                await context.SaveChangesAsync(ct);

                return Result.Success();
            }

            return Result.Failure("User ID not found.", userId);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> RemoveRolesAsync(Guid userId, IList<RoleDto> roles, Guid byUserId,
        CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var user = context.Users
                .AsNoTracking()
                .Include(user => user.Roles)
                .FirstOrDefault(x => x.Id == userId);
            if (user != null)
            {
                foreach (var userRole in user.Roles)
                {
                    if (roles.Any(x => x.Id == userRole.Id))
                        user.Roles.Remove(userRole);
                }

                user.SetUpdateChangeHistory(byUserId);
                context.Users.Update(user);
                await context.SaveChangesAsync(ct);

                return Result.Success();
            }

            return Result.Failure("User ID not found.", userId);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }
}