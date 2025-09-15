using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Infrastructure.Data.Abstractions;

namespace MiniIAM.Infrastructure.Data.Repositories.Roles.Abstractions;

public interface IRoleWriteRepository : IWriteRepository<Role, Guid, RoleDto>
{
    
}