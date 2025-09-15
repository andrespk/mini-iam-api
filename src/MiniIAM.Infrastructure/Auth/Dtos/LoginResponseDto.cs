namespace MiniIAM.Infrastructure.Auth.Dtos;

public record LoginResponseDto(bool IsLoggedIn, string AccessToken, string RefreshToken);