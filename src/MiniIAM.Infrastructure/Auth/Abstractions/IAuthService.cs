using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Infrastructure.Data;

namespace MiniIAM.Infrastructure.Auth.Abstractions;

public interface IAuthService
{
    Result<string> GenerateJwt(string userId);
    Result<LoginResponseDto> RefreshJwt(string refreshToken);
    Task<Result<LoginResponseDto>> LogInAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<Result> LogOutAsync(string accessToken, CancellationToken ct = default);
    Result<bool> IsJwtValid(string accessToken);
}