using MiniIAM.Domain.Sessions.Entities;

namespace MiniIAM.Domain.Sessions.Dtos;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime LastRefreshedAtUtc { get; set; }
    public bool IsActive { get; set; }

    public static SessionDto FromEntity(Session session)
    {
        return new SessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            StartedAtUtc = session.StartedAtUtc,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            LastRefreshedAtUtc = session.LastRefreshedAtUtc,
            IsActive = session.IsActive
        };
    }

    public Session ToEntity()
    {
        return new Session(UserId, AccessToken, RefreshToken)
        {
            Id = Id,
            StartedAtUtc = StartedAtUtc,
            LastRefreshedAtUtc = LastRefreshedAtUtc,
            IsActive = IsActive
        };
    }
}
