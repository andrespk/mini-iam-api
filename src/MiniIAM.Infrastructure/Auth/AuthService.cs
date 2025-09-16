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
    IUserReadRepository userReadRepository)
    : IAuthService
{
    public Result<string> GenerateJwt(string sub)
    {
        try
        {
            if (!Guid.TryParse(sub, out var userId))
                return Result.Failure("Invalid SUB.");

            var user = GetUserById(sub);
            
            if (user == null)
                return Result.Failure("Invalid SUB.");

            var claims = BuildClaims(userId, user.Email);
            var jwtKey = config["Jwt:Key"];
            
            if (string.IsNullOrEmpty(jwtKey))
                return Result.Failure("Missing JWT Key.");
            
            var forgedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(forgedKey, SecurityAlgorithms.HmacSha256);
            var token = BuildJwt(creds, claims);

            return Result<string>.Success(new JwtSecurityTokenHandler().WriteToken(token));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
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
            
            var user = GetUserById(sub);
            
            if (user == null)
                return Result.Failure("Invalid SUB.");

            var jwt = BuildJwt(creds, BuildClaims(userId, user.Email));

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            return Result<LoginResponseDto>.Success(new LoginResponseDto(true, accessToken, newRefreshRaw));
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
                    var refreshToken = NewRefreshToken();
                    var accessToken = GenerateJwt(request.Email).Data!;
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

    private Claim[] BuildClaims(Guid userId, string email) => new[]
    {
        new Claim("sub", userId.ToString()),
        new Claim("Email", email),
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