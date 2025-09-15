using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Domain.Users.Dtos;

namespace MiniIAM.Domain.Users.Entitties;

public class User(
    Guid id,
    string name,
    string email,
    string password,
    IList<Role>? roles = null,
    DataChangesHistory? changesHistory = null) : EntityBase<Guid>(id, changesHistory)
{
    public string Name { get; set; } = name;
    public string Password { get; set; } = password;
    public string Email { get; set; } = email;
    public IList<Role> Roles { get; set; } = roles ?? Array.Empty<Role>();

    public User(UserDto dto) : this(dto.Id, dto.Name, dto.Email, dto.Password,
        dto.Roles.Select(x => x.ToEntity()).ToList(), dto.ChangesHistory)
    {
    }

    public override UserDto ToDto() => new UserDto(Id, Name, Email, Password, Roles.Select(x => x.ToDto()).ToList(), ChangesHistory);
}