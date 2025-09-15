using MiniIAM.Domain.Users.Dtos;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Data.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;

public interface IUserWriteRepository : IWriteRepository<User, Guid, UserDto>
{
    Task<Result> SetPasswordAsync(Guid userId, string password, Guid byUserId, CancellationToken ct = default);
}