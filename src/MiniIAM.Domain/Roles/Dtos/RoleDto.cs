using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Domain.Users.Dtos;

namespace MiniIAM.Domain.Roles.Dtos;

public class RoleDto(Guid id, DataChangesHistory? changesHistory = null) : IEntityDto<Role>
{
    public Guid Id { get; } = id;
    public string Name {get; }
    
    public IList<UserDto> Users { get; }  
    private DataChangesHistory ChangesHistory { get; } = changesHistory ?? new DataChangesHistory();
    
    public RoleDto(Guid id, string name, IList<UserDto>? users, DataChangesHistory?  changesHistory) : this(id)
    {
        Name = name;
        Users = users ?? new List<UserDto>();
        ChangesHistory = changesHistory ?? new DataChangesHistory();
    }
    
    public Role ToEntity()
    {
        var role =  new Role(Id, Name, ChangesHistory);
        role.AddUsers(Users.Select(x => x.ToEntity()));
        return role;
    }

   
}