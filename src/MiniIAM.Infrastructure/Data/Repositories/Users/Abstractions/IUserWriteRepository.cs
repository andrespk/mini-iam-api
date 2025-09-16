using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Users.Dtos;
using MiniIAM.Domain.Users.Entitties;
using MiniIAM.Infrastructure.Data.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;

public interface IUserWriteRepository : IWriteRepository<User, Guid, UserDto>
{
    Task<Result> SetPasswordAsync(Guid userId, string password, Guid byUserId, CancellationToken ct = default);
    Task<Result> AddRolesAsync(Guid userId,IList<RoleDto> roles, Guid byUserId, CancellationToken ct = default);
    Task<Result> RemoveRolesAsync(Guid userId, IList<RoleDto> rolesIds, Guid byUserId, CancellationToken ct = default);
}