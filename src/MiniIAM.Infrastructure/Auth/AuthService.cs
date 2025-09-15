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

namespace MiniIAM.Infrastructure.Auth;

public sealed class AuthService(
    IConfiguration config,
    ILogger<AuthService> logger,
    ICachingService cacheService,
    IUserReadRepository repository)
    : IAuthService
{
    public Result<string> GenerateJwt(string sub)
    {
        try
        {
            if (!Guid.TryParse(sub, out var userId))
                return Result.Failure("Invalid sub.");

            var claims = BuildClaims(userId);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
            var token = BuildJwt(creds, claims);

            return Result.Success(new JwtSecurityTokenHandler().WriteToken(token));
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
}

    public Result<LoginResponseDto> RefreshJwt(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Result.Failure("Missing refresh token.");

            var refreshStore = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

            var key = HashValue(refreshToken);
            if (!refreshStore.TryRemove(key, out var sub))
                return Result.Failure("Invalid or expired refresh token.");

            if (!Guid.TryParse(sub, out var userId))
                return Result.Failure("Invalid SUB.");

            var newRefreshRaw = NewRefreshToken();
            var newRefreshHash = HashValue(newRefreshRaw);
            refreshStore[newRefreshHash] = userId.ToString();

            var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? string.Empty));
            var creds = new SigningCredentials(jwtKey, SecurityAlgorithms.HmacSha512);

            var jwt = BuildJwt(creds, BuildClaims(userId));

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            return Result<LoginResponseDto>.Success(new LoginResponseDto(true, accessToken, newRefreshRaw));
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result<LoginResponseDto>> LogInAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            
            var userDtos =
                (await repository.GetAsync(x => x.Email == request.Email || x.Name == request.Email,
                    null)).Data;
            
            if (userDtos != null)
            {
                var user = userDtos.FirstOrDefault()!;

                if (IsPasswordValid(request.Password, user))
                {
                    var refreshToken = NewRefreshToken();
                    var accessToken = GenerateJwt(request.Email).Data!;
                    return Result<LoginResponseDto>.Success(new LoginResponseDto(true, accessToken, refreshToken));
                }
            }
            
            return Result.Failure("User data not found.");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
    }

    public async Task<Result> LogOutAsync(string accessToken, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            IsJwtValid(accessToken);
            await AddJwtToBlackList(accessToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
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

    private Claim[] BuildClaims(Guid userId) => new[]
    {
        new Claim("sub", userId.ToString()),
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
    
    public Result<bool> IsJwtValid(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return Result.Failure("Missing access token.");

        var handler = new JwtSecurityTokenHandler();
        
        if (!handler.CanReadToken(accessToken))
            return Result.Failure("Invalid access token.");

        return Result<bool>.Success(true);
    }
    
    private string GetJwtBlackListKey(string accessToken) => $"blacklisted-jwt:{accessToken}";

    private bool IsPasswordValid(string password, UserDto user) =>
        !string.IsNullOrEmpty(password) && BCrypt.Net.BCrypt.Verify(password, user.Password);
}