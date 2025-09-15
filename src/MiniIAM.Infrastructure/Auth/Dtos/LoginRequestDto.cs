namespace MiniIAM.Infrastructure.Auth.Dtos;

public record LoginRequestDto(string Email, string Password, bool? IsFirstAccess);