using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Entities;
using MiniIAM.Domain.Users.Dtos;

namespace MiniIAM.Domain.Users.Entitties
{
    public class User : EntityBase<Guid>
    {
        private readonly List<Role> _roles = new();

        public string Name { get; private set; } = default!;
        public string Email { get; private set; } = default!;
        public string Password { get; private set; } = default!;

        public IReadOnlyCollection<Role> Roles => _roles;

        // Constructor for Entity Framework
        private User() : base(Guid.NewGuid(), new DataChangesHistory())
        {
            Name = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
        }

        public User(Guid id, string name, string email, string password, IEnumerable<Role> roles,
            DataChangesHistory changesHistory) : base(id, changesHistory)
        {
            SetName(name);
            SetEmail(email);
            SetPassword(password);
            ReplaceRoles(roles);
        }

        public static User FromDto(UserDto dto)
        {
            var u = new User(dto.Id, dto.Name, dto.Email, dto.Password, dto.Roles.Select(x => x.ToEntity()).ToList(),
                dto.ChangesHistory);
            foreach (var r in dto.Roles.Select(x => x.ToEntity()))
                u.AddRole(r);
            u.ChangesHistory = dto.ChangesHistory;
            return u;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));
            Name = name.Trim();
        }

        public void SetEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));
            Email = email.Trim().ToLowerInvariant();
            if (!Email.Contains('@')) throw new ArgumentException("Invalid email.", nameof(email));
        }

        public void SetPassword(string passwordOrHash)
        {
            if (string.IsNullOrWhiteSpace(passwordOrHash) || string.IsNullOrEmpty(passwordOrHash))
                throw new ArgumentException("Password is required.", nameof(passwordOrHash));

            if (passwordOrHash.Length < 6)
                throw new ArgumentException("Password should be at least 6 characters long.", nameof(passwordOrHash));

            // Se a senha já está hasheada (vem do seeder), usa diretamente
            if (Password != null && BCrypt.Net.BCrypt.Verify(passwordOrHash, Password))
            {
                // Senha já está correta, não precisa fazer nada
                return;
            }
            
            // Senha não está hasheada ou é diferente, então hash a nova senha
            Password = BCrypt.Net.BCrypt.HashPassword(passwordOrHash);
        }

        public void AddRole(Role role)
        {
            if (role is null) throw new ArgumentNullException(nameof(role));
            if (!_roles.Contains(role)) _roles.Add(role);
        }

        public void RemoveRole(Role role)
        {
            if (role is null) throw new ArgumentNullException(nameof(role));
            _roles.Remove(role);
        }

        public void ReplaceRoles(IEnumerable<Role> roles)
        {
            if (roles is null) throw new ArgumentNullException(nameof(roles));
            _roles.Clear();
            _roles.AddRange(roles);
        }

        public override UserDto ToDto()
            => new(Id, Name, Email, Password, _roles.Select(x => x.ToDto()).ToList(), ChangesHistory);
    }
}