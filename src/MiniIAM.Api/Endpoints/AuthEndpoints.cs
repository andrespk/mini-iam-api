using Asp.Versioning;
using MiniIAM.Application.UseCases.Auth;
using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MinimalCqrs;

namespace MiniIAM.Api.Endpoints;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var v1 = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/auth")
            .WithApiVersionSet(v1)
            .MapToApiVersion(1.0);

        group.MapPost("/login", async (ICommandDispatcher commands, LoginRequestDto request) =>
            {
                var result = await commands.DispatchAsync(new LogInUser.Command(request.Email, request.Password));
                if (!result.IsSuccess) return Results.Unauthorized();

                var payload = result.Value!;
                return Results.Ok(new LoginResponseDto(payload.AccessToken, payload.RefreshToken));
            })
            .WithSummary("Authenticate user")
            .WithDescription("Returns access and refresh tokens on success.")
            .Produces<LoginResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPost("/logout", async (ICommandDispatcher commands, HttpContext http, CancellationToken ct) =>
            {
                var auth = http.Request.Headers.Authorization.ToString();
                var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? auth["Bearer ".Length..]
                    : string.Empty;

                var result = await commands.Dispatch(new LogOutUser.Command(token), ct);
                if (!result.IsSuccess) return Results.BadRequest(result.Error?.Message ?? "Logout failed");

                return Results.Ok(new { success = true });
            })
            .WithSummary("Logout user")
            .WithDescription("Revokes the current access token.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .AllowAnonymous();
    }
}