using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Infrastructure.Data.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions;

public interface IRoleReadRepository : IReadRepository<Role, Guid, RoleDto>
{
    public Result<RoleDto> GetById(Guid id);
    public Result<RoleDto> GetByName(string name);
}