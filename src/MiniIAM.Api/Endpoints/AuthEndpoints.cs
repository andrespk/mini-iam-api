using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MinimalCqrs;
using MiniIAM.Infrastructure.Auth.Dtos;
using System.Threading.Tasks;

namespace MiniIAM.Api.Endpoints;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login", async (IMediator mediator, LoginRequestDto request, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LogInUser.Command(request.Email, request.Password), ct);
            if (!result.IsSuccess) return Results.Unauthorized();
            var payload = result.Value!;
            return Results.Ok(new LoginResponseDto(payload.AccessToken, payload.RefreshToken));
        }).AllowAnonymous();

        group.MapPost("/logout", async (IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            // Pull bearer token from Authorization header
            var auth = http.Request.Headers.Authorization.ToString();
            var token = auth.StartsWith("Bearer ") ? auth["Bearer ".Length..] : string.Empty;
            var result = await mediator.Send(new LogOutUser.Command(token), ct);
            if (!result.IsSuccess) return Results.BadRequest(result.Error?.Message ?? "Logout failed");
            return Results.Ok(new { success = true });
        }).AllowAnonymous();
    }
}
