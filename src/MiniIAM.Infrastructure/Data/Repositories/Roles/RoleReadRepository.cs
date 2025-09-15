using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Data.Paging;
using MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Roles;

public class RoleReadRepository(MainDbContext context) : IRoleReadRepository
{
    public async Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var entity = await context.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return entity != null
                ? Result<RoleDto>.Success(entity.ToDto())
                : Result.NoDataFound();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<ResultList<RoleDto>> GetPagedAsync(PageMeta pageMeta,
        Expression<Func<Role, bool>>? filter,
        DataSort? sort, string? searchText = null,
        CancellationToken? ct = default)
    {
        try
        {
            ct ??= CancellationToken.None;
            ct.Value.ThrowIfCancellationRequested();
            var queryable = context.Roles.AsNoTracking();

            if (filter != null)
                queryable = queryable.Where(filter!);

            if (sort != null)
                queryable = queryable.OrderBy(sort.GetStringDefinition());

            if (searchText != null)
                queryable = queryable.Where(x => x.Name.ToLower().Contains(searchText.ToLower()));

            pageMeta.Update(await queryable.CountAsync(ct.Value));

            var data = (await queryable.Skip((pageMeta.Page - 1) * pageMeta.PageSize).Take(pageMeta.PageSize)
                .ToListAsync(ct.Value)).Select(x => x.ToDto());

            return ResultList<RoleDto>.Success(data.ToList(), pageMeta);
        }
        catch (Exception ex)
        {
            return ResultList<RoleDto>.Failure(ex);
        }
    }

    public async Task<ResultList<RoleDto>> GetAsync(Expression<Func<Role, bool>>? filter, DataSort? sort,
        string? searchText = null, CancellationToken? ct = default)
    {
        try
        {
            ct ??= CancellationToken.None;
            ct.Value.ThrowIfCancellationRequested();
            var queryable = context.Roles.AsNoTracking();

            if (filter != null)
                queryable = queryable.Where(filter);

            if (sort != null)
                queryable = queryable.OrderBy(sort.GetStringDefinition());

            if (searchText != null)
            {
                queryable = queryable.Where(x => x.Name.ToLower().Contains(searchText.ToLower()));
            }

            var data = (await queryable.ToListAsync(ct.Value)).Select(x => x.ToDto());

            return ResultList<RoleDto>.Success(data.ToList(), null);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }


    public Result<RoleDto> GetById(Guid id)
    {
        try
        {
            var entity = context.Roles.AsNoTracking().FirstOrDefault(x => x.Id == id);
            return entity != null
                ? Result<RoleDto>.Success(entity.ToDto())
                : Result.NoDataFound();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public Result<RoleDto> GetByName(string name)
    {
        try
        {
            var entity = context.Roles.AsNoTracking().FirstOrDefault(x => x.Name == name);
            return entity != null
                ? Result<RoleDto>.Success(entity.ToDto())
                : Result.NoDataFound();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }
}