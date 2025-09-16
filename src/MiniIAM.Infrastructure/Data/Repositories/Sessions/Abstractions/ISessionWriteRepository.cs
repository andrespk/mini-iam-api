using MiniIAM.Domain.Sessions.Entities;
using MiniIAM.Infrastructure.Data.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Sessions.Abstractions;

public interface ISessionWriteRepository : IWriteRepository<Session, Guid, MiniIAM.Domain.Sessions.Dtos.SessionDto>
{
    Task<Result<Session>> CreateSessionAsync(Session session, CancellationToken ct = default);
    Task<Result<Session>> UpdateSessionTokensAsync(Guid sessionId, string accessToken, string refreshToken, CancellationToken ct = default);
    Task<Result<Session>> DeactivateSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<Result> DeactivateSessionsByUserIdAsync(Guid userId, CancellationToken ct = default);
}
