using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniIAM.Infrastructure.Auth;
using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Infrastructure.Data;
using MiniIAM.Infrastructure.Caching.Abstractions;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;
using MiniIAM.Infrastructure.Data.Repositories.Sessions.Abstractions;
using MiniIAM.Domain.Sessions.Dtos;
using MiniIAM.Domain.Users.Dtos;
using MiniIAM.Domain.Abstractions;
using MiniIAM.Domain.Roles.Dtos;
using Moq;
using System.Collections.Generic;

namespace MiniIAM.Tests.Unit;

public class AuthServiceSessionTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<ICachingService> _cacheServiceMock;
    private readonly Mock<IUserReadRepository> _userReadRepositoryMock;
    private readonly Mock<ISessionReadRepository> _sessionReadRepositoryMock;
    private readonly Mock<ISessionWriteRepository> _sessionWriteRepositoryMock;
    private readonly AuthService _authService;

    public AuthServiceSessionTests()
    {
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _cacheServiceMock = new Mock<ICachingService>();
        _userReadRepositoryMock = new Mock<IUserReadRepository>();
        _sessionReadRepositoryMock = new Mock<ISessionReadRepository>();
        _sessionWriteRepositoryMock = new Mock<ISessionWriteRepository>();

        _configMock.Setup(x => x["Jwt:Key"]).Returns("test-jwt-key-that-is-at-least-32-characters-long");
        _configMock.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
        _configMock.Setup(x => x["Jwt:Audience"]).Returns("test-audience");
        _configMock.Setup(x => x["Jwt:ExpireMinutes"]).Returns("20");

        _authService = new AuthService(
            _configMock.Object,
            _loggerMock.Object,
            _cacheServiceMock.Object,
            _userReadRepositoryMock.Object,
            _sessionReadRepositoryMock.Object,
            _sessionWriteRepositoryMock.Object);
    }

    [Fact]
    public void GenerateJwt_WithSessionId_ShouldIncludeSessionClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var userDto = new UserDto(userId, "Test User", "test@example.com", "hashedpassword", new List<RoleDto>(), new DataChangesHistory());

        _userReadRepositoryMock.Setup(x => x.GetById(userId))
            .Returns(Result<UserDto>.Success(userDto));

        // Act
        var result = _authService.GenerateJwt(userId.ToString(), sessionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateJwt_WithoutSessionId_ShouldStillWork()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserDto(userId, "Test User", "test@example.com", "hashedpassword", new List<RoleDto>(), new DataChangesHistory());

        _userReadRepositoryMock.Setup(x => x.GetById(userId))
            .Returns(Result<UserDto>.Success(userDto));

        // Act
        var result = _authService.GenerateJwt(userId.ToString());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshJwtAsync_WithValidSession_ShouldUpdateTokens()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var sessionDto = new SessionDto
        {
            Id = sessionId,
            UserId = userId,
            AccessToken = "old_access_token",
            RefreshToken = refreshToken,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            LastRefreshedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        };

        _sessionReadRepositoryMock
            .Setup(x => x.GetByAccessTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SessionDto>.Success(sessionDto));

        _sessionWriteRepositoryMock
            .Setup(x => x.UpdateSessionTokensAsync(sessionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MiniIAM.Domain.Sessions.Entities.Session>.Success(new MiniIAM.Domain.Sessions.Entities.Session(userId, "", "")));

        var userDto = new UserDto(userId, "Test User", "test@example.com", "hashedpassword", new List<RoleDto>(), new DataChangesHistory());
        _userReadRepositoryMock.Setup(x => x.GetById(userId))
            .Returns(Result<UserDto>.Success(userDto));

        // Act
        var result = await _authService.RefreshJwtAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.IsLoggedIn.Should().BeTrue();

        _sessionWriteRepositoryMock.Verify(
            x => x.UpdateSessionTokensAsync(sessionId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshJwtAsync_WithExpiredSession_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = "expired_refresh_token";
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var sessionDto = new SessionDto
        {
            Id = sessionId,
            UserId = userId,
            AccessToken = "old_access_token",
            RefreshToken = refreshToken,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            LastRefreshedAtUtc = DateTime.UtcNow.AddMinutes(-25), // Mais de 20 minutos atrás
            IsActive = true
        };

        _sessionReadRepositoryMock
            .Setup(x => x.GetByAccessTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SessionDto>.Success(sessionDto));

        _sessionWriteRepositoryMock
            .Setup(x => x.DeactivateSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MiniIAM.Domain.Sessions.Entities.Session>.Success(new MiniIAM.Domain.Sessions.Entities.Session(userId, "", "")));

        // Act
        var result = await _authService.RefreshJwtAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Notifications.GetStringfiedList().Should().Contain("Session expired");

        _sessionWriteRepositoryMock.Verify(
            x => x.DeactivateSessionAsync(sessionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshJwtAsync_WithInactiveSession_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = "inactive_refresh_token";
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var sessionDto = new SessionDto
        {
            Id = sessionId,
            UserId = userId,
            AccessToken = "old_access_token",
            RefreshToken = refreshToken,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            LastRefreshedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsActive = false // Sessão inativa
        };

        _sessionReadRepositoryMock
            .Setup(x => x.GetByAccessTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SessionDto>.Success(sessionDto));

        // Act
        var result = await _authService.RefreshJwtAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Notifications.GetStringfiedList().Should().Contain("Session is inactive");
    }

    [Fact]
    public async Task LogOutAsync_WithValidAccessToken_ShouldDeactivateSession()
    {
        // Arrange
        var accessToken = "valid_access_token";
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var sessionDto = new SessionDto
        {
            Id = sessionId,
            UserId = userId,
            AccessToken = accessToken,
            RefreshToken = "refresh_token",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            LastRefreshedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        };

        _sessionReadRepositoryMock
            .Setup(x => x.GetByAccessTokenAsync(accessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SessionDto>.Success(sessionDto));

        _sessionWriteRepositoryMock
            .Setup(x => x.DeactivateSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MiniIAM.Domain.Sessions.Entities.Session>.Success(new MiniIAM.Domain.Sessions.Entities.Session(userId, "", "")));

        _cacheServiceMock
            .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LogOutAsync(accessToken);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _sessionWriteRepositoryMock.Verify(
            x => x.DeactivateSessionAsync(sessionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
