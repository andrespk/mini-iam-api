using Microsoft.EntityFrameworkCore;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Roles;

public class RoleWriteRepository(MainDbContext context) : IRoleWriteRepository
{
    public async Task<Result> InsertAsync(Role entity, Guid byRoleId, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            entity.SetInsertChangeHistory(byRoleId);
            context.Roles.Add(entity);
            await context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> UpdateAsync(Role entity, Guid byRoleId, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            entity.SetUpdateChangeHistory(byRoleId);
            context.Roles.Update(entity);
            await context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, Guid byRoleId, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var entity = await context.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

            if (entity is null) return Result.NoDataFound();

            entity.DeleteChangeHistory(byRoleId);
            context.Roles.Update(entity);
            await context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }
}