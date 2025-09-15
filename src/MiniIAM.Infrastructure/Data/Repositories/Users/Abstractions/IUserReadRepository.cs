using MiniIAM.Domain.Users.Dtos;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Data.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;

public interface IUserReadRepository : IReadRepository<User, Guid, UserDto>
{
    public Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default);
}