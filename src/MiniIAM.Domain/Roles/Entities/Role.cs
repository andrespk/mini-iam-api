using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Users.Entitties;

namespace MiniIAM.Domain.Roles.Entities;

public class Role(Guid id, string name, IList<User>? users, DataChangesHistory? changesHistory)
    : EntityBase<Guid>(id, changesHistory)
{
    public string Name { get; } = name;
    public IList<User> Users { get; } = users ?? Array.Empty<User>();
    public override RoleDto ToDto() => new(Id, Name, Users.Select(x => x.ToDto()).ToList(), ChangesHistory);
}