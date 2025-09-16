using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MiniIAM.Domain.Users.Dtos;
using MiniIAM.Infrastructure.Auth.Abstractions;
using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Infrastructure.Caching.Abstractions;
using MiniIAM.Infrastructure.Data;
using MiniIAM.Infrastructure.Data.Repositories.Users.Abstractions;
using MiniIAM.Infrastructure.Data.Repositories.Sessions.Abstractions;

namespace MiniIAM.Infrastructure.Auth;

public sealed class AuthService(
    IConfiguration config,
    ILogger<AuthService> logger,
    ICachingService cacheService,
    IUserReadRepository userReadRepository,
    ISessionReadRepository sessionReadRepository,
    ISessionWriteRepository sessionWriteRepository)
    : IAuthService
{
    public Result<string> GenerateJwt(string sub, Guid? sessionId = null)
    {
        try
        {
            if (!Guid.TryParse(sub, out var userId))
                return Result.Failure("Invalid SUB.");

            var claims = BuildClaims(userId, sub, sessionId); // Usar sub como email temporariamente
            var jwtKey = config["Jwt:Key"];
            
            if (string.IsNullOrEmpty(jwtKey))
                return Result.Failure("Missing JWT Key.");
            
            var forgedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(forgedKey, SecurityAlgorithms.HmacSha256);
            var token = BuildJwt(creds, claims);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            logger.LogInformation("Generated JWT token: {Token}", tokenString);
            
            return Result<string>.Success(tokenString);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result<LoginResponseDto>> RefreshJwtAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result.Failure("Missing refresh token.");

            // Buscar sessão pelo refresh token
            var sessionResult = await sessionReadRepository.GetByAccessTokenAsync(refreshToken, ct);
            if (!sessionResult.IsSuccess || sessionResult.Data == null)
                return Result.Failure("Invalid or expired refresh token.");

            var session = sessionResult.Data;
            if (!session.IsActive)
                return Result.Failure("Session is inactive.");

            // Verificar se a sessão não expirou (20 minutos)
            var sessionExpiration = TimeSpan.FromMinutes(20);
            if (DateTime.UtcNow - session.LastRefreshedAtUtc > sessionExpiration)
            {
                // Desativar sessão expirada
                await sessionWriteRepository.DeactivateSessionAsync(session.Id, ct);
                return Result.Failure("Session expired.");
            }

            // Gerar novos tokens
            var newRefreshToken = NewRefreshToken();
            var newAccessToken = GenerateJwt(session.UserId.ToString(), session.Id).Data!;

            // Atualizar sessão com novos tokens
            var updateResult = await sessionWriteRepository.UpdateSessionTokensAsync(
                session.Id, newAccessToken, newRefreshToken, ct);

            if (!updateResult.IsSuccess)
                return Result.Failure("Failed to update session tokens.");

            return Result<LoginResponseDto>.Success(new LoginResponseDto(true, newAccessToken, newRefreshToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result<LoginResponseDto>> LogInAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            
            var userDtos =
                (await userReadRepository.GetAsync(x => x.Email == request.Email || x.Name == request.Email,
                    null)).Data;
            
            if (userDtos != null)
            {
                var user = userDtos.FirstOrDefault()!;

                if (IsPasswordValid(request.Password, user))
                {
                    // Verificar e desativar sessões expiradas do usuário
                    var expiredSessions = await sessionReadRepository.GetExpiredSessionsByUserIdAsync(
                        user.Id, TimeSpan.FromMinutes(20), ct);
                    
                    if (expiredSessions.IsSuccess && expiredSessions.Data.Any())
                    {
                        await sessionWriteRepository.DeactivateSessionsByUserIdAsync(user.Id, ct);
                    }

                    // Criar nova sessão
                    var refreshToken = NewRefreshToken();
                    var session = new MiniIAM.Domain.Sessions.Entities.Session(user.Id, "", refreshToken);
                    var sessionResult = await sessionWriteRepository.CreateSessionAsync(session, ct);
                    
                    if (!sessionResult.IsSuccess)
                        return Result.Failure("Failed to create session.");

                    // Gerar access token com ID da sessão
                    var accessToken = GenerateJwt(user.Id.ToString(), session.Id).Data!;
                    
                    // Atualizar sessão com access token
                    await sessionWriteRepository.UpdateSessionTokensAsync(session.Id, accessToken, refreshToken, ct);

                    return Result<LoginResponseDto>.Success(new LoginResponseDto(true, accessToken, refreshToken));
                }
            }
            
            return Result.Failure("User data not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
            return Result.Failure(ex);
        }
    }

    public async Task<Result> LogOutAsync(string accessToken, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            IsJwtValid(accessToken);
            
            // Buscar sessão pelo access token e desativar
            var sessionResult = await sessionReadRepository.GetByAccessTokenAsync(accessToken, ct);
            if (sessionResult.IsSuccess && sessionResult.Data != null)
            {
                await sessionWriteRepository.DeactivateSessionAsync(sessionResult.Data.Id, ct);
            }
            
            await AddJwtToBlackList(accessToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
            return Result.Failure(ex);
        }
    }
    
    public Result<bool> IsJwtValid(string accessToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return Result.Failure("Missing access token.");

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(accessToken))
                return Result.Failure("Invalid access token.");

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
            return Result.Failure(ex);
        }
    }
    
    private string NewRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private string HashValue(string s)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }

    private JwtSecurityToken BuildJwt(SigningCredentials creds, Claim[] claims, int? expireInMinutes = null)
    {
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expireInMinutes.HasValue
                ? expireInMinutes.GetValueOrDefault()
                : int.Parse(config["Jwt:ExpireMinutes"] ?? "20")),
            signingCredentials: creds);
        return token;
    }

    private Claim[] BuildClaims(Guid userId, string email, Guid? sessionId = null) => new[]
    {
        new Claim("sub", userId.ToString()),
        new Claim("Email", email),
        new Claim("session_id", sessionId?.ToString() ?? string.Empty),
    };

    private bool IsTokenRevoked(string accessToken)
    {
        var key = GetJwtBlackListKey(accessToken);
        return cacheService.KeyExists(key);
    }

    private async Task AddJwtToBlackList(string accessToken)
    {
        IsJwtValid(accessToken);

        var key = GetJwtBlackListKey(accessToken);
        var blackList = await cacheService.GetAsync<string>(key);
        if (blackList is null)
            await cacheService.SetAsync(key, DateTime.UtcNow, TimeSpan.FromHours(24));
    }
    
    private string GetJwtBlackListKey(string accessToken) => $"blacklisted-jwt:{accessToken}";

    private bool IsPasswordValid(string password, UserDto user) =>
        !string.IsNullOrEmpty(password) && BCrypt.Net.BCrypt.Verify(password, user.Password);

    private UserDto? GetUserById(string id)
    {
        var result = userReadRepository.GetById(Guid.Parse(id));
        return result.IsSuccess ? result.Data : null;
    }
        
}