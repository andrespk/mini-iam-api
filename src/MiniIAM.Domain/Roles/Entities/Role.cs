using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Domain.Users.Entitties;

namespace MiniIAM.Domain.Roles.Entities;

public class Role : EntityBase<Guid>
{
    private readonly List<User> _users = new();

    public Role(Guid id, string name, DataChangesHistory? changesHistory) : base(id, changesHistory)
    {
        Name = name;
    }

    public string Name { get; set; }
    public IReadOnlyCollection<User> Users => _users;

    public void AddUsers(IEnumerable<User> users)
    {
        foreach (var user in users)
        {
            if(_users.All(x => x.Id != user.Id))
                _users.Add(user);
        }
    }
   
    private Role(Guid id, string name) : base(id, null)
    {
        Name = name;
    }
    
    public void AddUser(User user)
    {
        if(_users.All(x => x.Id != user.Id))
            _users.Add(user);
    }
    public override RoleDto ToDto() => new(Id, Name, Users.Select(x => x.ToDto()).ToList(), ChangesHistory);
    
}