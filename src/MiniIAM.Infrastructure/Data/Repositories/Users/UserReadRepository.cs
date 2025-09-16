using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniIAM.Domain.Users.Dtos;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Data.Paging;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Users;

public class UserReadRepository(MainDbContext context) : IUserReadRepository
{
    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var entity = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return entity != null
                ? Result<UserDto>.Success(entity.ToDto())
                : Result.NoDataFound();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public Result<UserDto> GetById(Guid id)
    {
        try
        {
            var entity = context.Users.AsNoTracking().FirstOrDefault(x => x.Id == id);
            return entity != null
                ? Result<UserDto>.Success(entity.ToDto())
                : Result.NoDataFound();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }


    public async Task<ResultList<UserDto>> GetPagedAsync(PageMeta pageMeta,
        Expression<Func<User, bool>>? filter,
        DataSort? sort, string? searchText = null,
        CancellationToken? ct = default)
    {
        try
        {
            ct ??= CancellationToken.None;
            ct.Value.ThrowIfCancellationRequested();
            var queryable = context.Users.AsNoTracking();

            if (filter != null)
                queryable = queryable.Where(filter!);

            if (sort != null)
                queryable = queryable.OrderBy(sort.GetStringDefinition());

            if (searchText != null)
            {
                queryable = queryable.Where(x =>
                    x.Email.ToLower().Contains(searchText.ToLower()) ||
                    x.Name.ToLower().Contains(searchText.ToLower()));
            }

            pageMeta.Update(await queryable.CountAsync(ct.Value));

            var data = (await queryable.Skip((pageMeta.Page - 1) * pageMeta.PageSize).Take(pageMeta.PageSize)
                .ToListAsync(ct.Value)).Select(x => x.ToDto());

            return ResultList<UserDto>.Success(data.ToList(), pageMeta);
        }
        catch (Exception ex)
        {
            return ResultList<UserDto>.Failure(ex);
        }
    }

    public async Task<ResultList<UserDto>> GetAsync(Expression<Func<User, bool>>? filter, DataSort? sort,
        string? searchText = null, CancellationToken? ct = default)
    {
        try
        {
            ct ??= CancellationToken.None;
            ct.Value.ThrowIfCancellationRequested();
            var queryable = context.Users.AsNoTracking();

            if (filter != null)
                queryable = queryable.Where(filter);

            if (sort != null)
                queryable = queryable.OrderBy(sort.GetStringDefinition());

            if (searchText != null)
            {
                queryable = queryable.Where(x =>
                    x.Email.ToLower().Contains(searchText.ToLower()) ||
                    x.Name.ToLower().Contains(searchText.ToLower()));
            }

            var data = (await queryable.ToListAsync(ct.Value)).Select(x => x.ToDto());

            return ResultList<UserDto>.Success(data.ToList(), null);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var entity = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, ct);
            return entity != null
                ? Result<UserDto>.Success(entity.ToDto())
                : Result.NoDataFound();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }
}