using MiniIAM.Domain.Sessions.Dtos;
using MiniIAM.Infrastructure.Data.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Sessions.Abstractions;

public interface ISessionReadRepository : IReadRepository<MiniIAM.Domain.Sessions.Entities.Session, Guid, SessionDto>
{
    Task<Result<SessionDto>> GetByAccessTokenAsync(string accessToken, CancellationToken ct = default);
    Task<ResultList<SessionDto>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<ResultList<SessionDto>> GetExpiredSessionsByUserIdAsync(Guid userId, TimeSpan expirationTime, CancellationToken ct = default);
}
