using MiniIAM.Domain.Abstractions;

namespace MiniIAM.Domain.Sessions.Entities;

public class Session : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime LastRefreshedAtUtc { get; set; }
    public bool IsActive { get; set; }

    // Constructor for Entity Framework
    private Session() : base(Guid.NewGuid(), new DataChangesHistory())
    {
        StartedAtUtc = DateTime.UtcNow;
        LastRefreshedAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    public Session(Guid userId, string accessToken, string refreshToken) : this()
    {
        UserId = userId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public void UpdateTokens(string newAccessToken, string newRefreshToken)
    {
        AccessToken = newAccessToken;
        RefreshToken = newRefreshToken;
        LastRefreshedAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    public void Invalidate()
    {
        IsActive = false;
    }

    public bool IsExpired(TimeSpan expirationTime)
    {
        return DateTime.UtcNow - LastRefreshedAtUtc > expirationTime;
    }

    public override object ToDto()
    {
        return new Dtos.SessionDto
        {
            Id = Id,
            UserId = UserId,
            StartedAtUtc = StartedAtUtc,
            AccessToken = AccessToken,
            RefreshToken = RefreshToken,
            LastRefreshedAtUtc = LastRefreshedAtUtc,
            IsActive = IsActive
        };
    }
}
