using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Users.Entitties;

namespace MiniIAM.Domain.Users.Dtos;

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Password,
    IList<RoleDto> Roles,
    DataChangesHistory ChangesHistory) : IEntityDto<User>
{
    public UserDto(User user) : this(user.Id, user.Name, user.Email, user.Password,
        user.Roles.Select(x => x.ToDto()).ToList(), user.ChangesHistory)
    {
    }

    public User ToEntity() => new(Id, Name, Email, Password, Roles.Select(x => x.ToEntity()).ToList(), ChangesHistory);
}