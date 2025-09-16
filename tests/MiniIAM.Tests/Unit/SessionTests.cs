using Xunit;
using FluentAssertions;
using MiniIAM.Domain.Sessions.Entities;
using MiniIAM.Domain.Sessions.Dtos;

namespace MiniIAM.Tests.Unit;

public class SessionTests
{
    [Fact]
    public void Session_Constructor_ShouldSetDefaultValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accessToken = "access_token";
        var refreshToken = "refresh_token";

        // Act
        var session = new Session(userId, accessToken, refreshToken);

        // Assert
        session.Id.Should().NotBeEmpty();
        session.UserId.Should().Be(userId);
        session.AccessToken.Should().Be(accessToken);
        session.RefreshToken.Should().Be(refreshToken);
        session.StartedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        session.LastRefreshedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        session.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateTokens_ShouldUpdateTokensAndTimestamp()
    {
        // Arrange
        var session = new Session(Guid.NewGuid(), "old_access", "old_refresh");
        var newAccessToken = "new_access";
        var newRefreshToken = "new_refresh";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        session.UpdateTokens(newAccessToken, newRefreshToken);

        // Assert
        session.AccessToken.Should().Be(newAccessToken);
        session.RefreshToken.Should().Be(newRefreshToken);
        session.LastRefreshedAtUtc.Should().BeOnOrAfter(beforeUpdate);
        session.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var session = new Session(Guid.NewGuid(), "access", "refresh");
        session.IsActive.Should().BeTrue();

        // Act
        session.Invalidate();

        // Assert
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithRecentTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var session = new Session(Guid.NewGuid(), "access", "refresh");
        var shortExpiration = TimeSpan.FromMinutes(1);

        // Act
        var isExpired = session.IsExpired(shortExpiration);

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithOldTimestamp_ShouldReturnTrue()
    {
        // Arrange
        var session = new Session(Guid.NewGuid(), "access", "refresh");
        // Simular uma sessão antiga
        session.UpdateTokens("new_access", "new_refresh");
        var oldTime = DateTime.UtcNow.AddMinutes(-30);
        var sessionType = typeof(Session);
        var property = sessionType.GetProperty("LastRefreshedAtUtc");
        property?.SetValue(session, oldTime);

        var shortExpiration = TimeSpan.FromMinutes(1);

        // Act
        var isExpired = session.IsExpired(shortExpiration);

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void SessionDto_FromEntity_ShouldMapCorrectly()
    {
        // Arrange
        var session = new Session(Guid.NewGuid(), "access", "refresh");

        // Act
        var dto = SessionDto.FromEntity(session);

        // Assert
        dto.Id.Should().Be(session.Id);
        dto.UserId.Should().Be(session.UserId);
        dto.AccessToken.Should().Be(session.AccessToken);
        dto.RefreshToken.Should().Be(session.RefreshToken);
        dto.StartedAtUtc.Should().Be(session.StartedAtUtc);
        dto.LastRefreshedAtUtc.Should().Be(session.LastRefreshedAtUtc);
        dto.IsActive.Should().Be(session.IsActive);
    }

    [Fact]
    public void SessionDto_ToEntity_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new SessionDto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AccessToken = "access",
            RefreshToken = "refresh",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            LastRefreshedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        };

        // Act
        var session = dto.ToEntity();

        // Assert
        session.Id.Should().Be(dto.Id);
        session.UserId.Should().Be(dto.UserId);
        session.AccessToken.Should().Be(dto.AccessToken);
        session.RefreshToken.Should().Be(dto.RefreshToken);
        session.StartedAtUtc.Should().Be(dto.StartedAtUtc);
        session.LastRefreshedAtUtc.Should().Be(dto.LastRefreshedAtUtc);
        session.IsActive.Should().Be(dto.IsActive);
    }
}
