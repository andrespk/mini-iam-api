using Microsoft.EntityFrameworkCore;
using MiniIAM.Domain.Sessions.Entities;
using MiniIAM.Infrastructure.Data.Contexts;
using MiniIAM.Infrastructure.Data.Repositories.Sessions.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Sessions;

public class SessionWriteRepository(MainDbContext context) : ISessionWriteRepository
{
    public async Task<Result> InsertAsync(Session entity, Guid byUserId, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            entity.SetInsertChangeHistory(byUserId);
            context.Sessions.Add(entity);
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> UpdateAsync(Session entity, Guid byUserId, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var existingSession = await context.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.Id);

            if (existingSession is null) return Result.Failure("Session ID not found.", entity.Id);

            existingSession.UpdateChangeHistory(byUserId);
            context.Sessions.Update(existingSession);
            await context.SaveChangesAsync(ct);

            return Result.Success();
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
            var entity = await context.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

            if (entity is null) return Result.NoDataFound();

            entity.DeleteChangeHistory(byUserId);
            context.Sessions.Update(entity);
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result<Session>> CreateSessionAsync(Session session, CancellationToken ct = default)
    {
        try
        {
            context.Sessions.Add(session);
            await context.SaveChangesAsync(ct);
            return Result<Session>.Success(session);
        }
        catch (Exception ex)
        {
            return Result<Session>.Failure($"Error creating session: {ex.Message}");
        }
    }

    public async Task<Result<Session>> UpdateSessionTokensAsync(Guid sessionId, string accessToken, string refreshToken, CancellationToken ct = default)
    {
        try
        {
            var session = await context.Sessions.FindAsync(sessionId);
            if (session == null)
                return Result<Session>.Failure("Session not found");

            session.UpdateTokens(accessToken, refreshToken);
            await context.SaveChangesAsync(ct);
            return Result<Session>.Success(session);
        }
        catch (Exception ex)
        {
            return Result<Session>.Failure($"Error updating session tokens: {ex.Message}");
        }
    }

    public async Task<Result<Session>> DeactivateSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            var session = await context.Sessions.FindAsync(sessionId);
            if (session == null)
                return Result<Session>.Failure("Session not found");

            session.Deactivate();
            await context.SaveChangesAsync(ct);
            return Result<Session>.Success(session);
        }
        catch (Exception ex)
        {
            return Result<Session>.Failure($"Error deactivating session: {ex.Message}");
        }
    }

    public async Task<Result> DeactivateSessionsByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var sessions = await context.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync(ct);

            foreach (var session in sessions)
            {
                session.Deactivate();
            }

            await context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error deactivating sessions: {ex.Message}");
        }
    }
}
