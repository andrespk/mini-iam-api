using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Domain.Users.Dtos;

namespace MiniIAM.Domain.Roles.Dtos;

public record RoleDto(
    Guid Id,
    string Name,
    IList<UserDto>? Users,
    DataChangesHistory? ChangesHistory) : IEntityDto<Role>
{
    public Role ToEntity()
    {
        var role =  new Role(Id, Name, ChangesHistory);
        if (Users != null) role.AddUsers(Users.Select(x => x.ToEntity()));
        return role;
    }
}