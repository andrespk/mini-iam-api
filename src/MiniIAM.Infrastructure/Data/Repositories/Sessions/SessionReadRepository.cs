using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniIAM.Domain.Sessions.Dtos;
using MiniIAM.Domain.Sessions.Entities;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Data.Paging;
using MiniIAM.Infrastructure.Data.Repositories.Sessions.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Sessions;

public class SessionReadRepository(MainDbContext context) : ISessionReadRepository
{
    public async Task<Result<SessionDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var entity = await context.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return entity != null
                ? Result<SessionDto>.Success(SessionDto.FromEntity(entity))
                : Result.NoDataFound();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public Result<SessionDto> GetById(Guid id)
    {
        try
        {
            var entity = context.Sessions.AsNoTracking().FirstOrDefault(x => x.Id == id);
            return entity != null
                ? Result<SessionDto>.Success(SessionDto.FromEntity(entity))
                : Result.NoDataFound();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<ResultList<SessionDto>> GetPagedAsync(PageMeta pageMeta,
        Expression<Func<Session, bool>>? filter,
        DataSort? sort, string? searchText = null,
        CancellationToken? ct = default)
    {
        try
        {
            ct ??= CancellationToken.None;
            ct.Value.ThrowIfCancellationRequested();
            var queryable = context.Sessions.AsNoTracking();

            if (filter != null)
                queryable = queryable.Where(filter!);

            if (sort != null)
                queryable = queryable.OrderBy(sort.GetStringDefinition());

            if (searchText != null)
            {
                queryable = queryable.Where(x =>
                    x.AccessToken.ToLower().Contains(searchText.ToLower()) ||
                    x.RefreshToken.ToLower().Contains(searchText.ToLower()));
            }

            pageMeta.Update(await queryable.CountAsync(ct.Value));

            var data = (await queryable.Skip((pageMeta.Page - 1) * pageMeta.PageSize).Take(pageMeta.PageSize)
                .ToListAsync(ct.Value)).Select(x => SessionDto.FromEntity(x));

            return ResultList<SessionDto>.Success(data.ToList(), pageMeta);
        }
        catch (Exception ex)
        {
            return ResultList<SessionDto>.Failure(ex);
        }
    }

    public async Task<ResultList<SessionDto>> GetAsync(Expression<Func<Session, bool>>? filter, DataSort? sort,
        string? searchText = null, CancellationToken? ct = default)
    {
        try
        {
            ct ??= CancellationToken.None;
            ct.Value.ThrowIfCancellationRequested();
            var queryable = context.Sessions.AsNoTracking();

            if (filter != null)
                queryable = queryable.Where(filter);

            if (sort != null)
                queryable = queryable.OrderBy(sort.GetStringDefinition());

            if (searchText != null)
            {
                queryable = queryable.Where(x =>
                    x.AccessToken.ToLower().Contains(searchText.ToLower()) ||
                    x.RefreshToken.ToLower().Contains(searchText.ToLower()));
            }

            var data = (await queryable.ToListAsync(ct.Value)).Select(x => SessionDto.FromEntity(x));

            return ResultList<SessionDto>.Success(data.ToList(), null);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result<SessionDto>> GetByAccessTokenAsync(string accessToken, CancellationToken ct = default)
    {
        try
        {
            var session = await context.Sessions
                .FirstOrDefaultAsync(s => s.AccessToken == accessToken && s.IsActive, ct);

            if (session == null)
                return Result<SessionDto>.Failure("Session not found");

            return Result<SessionDto>.Success(SessionDto.FromEntity(session));
        }
        catch (Exception ex)
        {
            return Result<SessionDto>.Failure($"Error retrieving session: {ex.Message}");
        }
    }

    public async Task<ResultList<SessionDto>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var sessions = await context.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .Select(s => SessionDto.FromEntity(s))
                .ToListAsync(ct);

            return ResultList<SessionDto>.Success(sessions, null);
        }
        catch (Exception ex)
        {
            return ResultList<SessionDto>.Failure($"Error retrieving active sessions: {ex.Message}");
        }
    }

    public async Task<ResultList<SessionDto>> GetExpiredSessionsByUserIdAsync(Guid userId, TimeSpan expirationTime, CancellationToken ct = default)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - expirationTime;
            var sessions = await context.Sessions
                .Where(s => s.UserId == userId && s.LastRefreshedAtUtc < cutoffTime)
                .Select(s => SessionDto.FromEntity(s))
                .ToListAsync(ct);

            return ResultList<SessionDto>.Success(sessions, null);
        }
        catch (Exception ex)
        {
            return ResultList<SessionDto>.Failure($"Error retrieving expired sessions: {ex.Message}");
        }
    }
}
