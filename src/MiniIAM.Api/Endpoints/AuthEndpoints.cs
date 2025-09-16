// ===============================
// File: src/MiniIAM.Api/Endpoints/AuthEndpoints.cs
// ===============================

using Asp.Versioning;
using Mapster;
using MiniIAM.Application.UseCases.Auth;
using MiniIAM.Infrastructure.Auth.Dtos;
using MiniIAM.Infrastructure.Cqrs.Abstractions;

namespace Movies.Endpoints;

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
            .MapToApiVersion(1.0)
            .WithTags("Auth");

        group.MapPost("/login", async (
                ICommandDispatcher commands,
                LoginRequestDto request,
                CancellationToken ct) =>
            {
                var command = new LogInUser.Command(request.Email, request.Password);
                var response = await commands.DispatchAsync<LogInUser.Command, LogInUser.Response>(command, ct);

                if (!response.IsLoggedIn)
                    return Results.Unauthorized();
                
                return Results.Ok(response.Adapt<LogInUser.Response>());
            })
            .WithSummary("Authenticate user")
            .WithDescription("Returns access and refresh tokens on success.")
            .Produces<LoginResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPost("/logout", async (
                HttpRequest http,
                ICommandDispatcher commands,
                CancellationToken ct) =>
            {
                var authHeader = http.Headers.Authorization.ToString();
                var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authHeader["Bearer ".Length..].Trim()
                    : authHeader.Trim();

                if (string.IsNullOrWhiteSpace(token))
                    return Results.Unauthorized();

                var deletedAtUtc = await commands.DispatchAsync<LogOutUser.Command, DateTime>(new LogOutUser.Command(token), ct);
                
                return Results.Ok(new { loggedOutAt = deletedAtUtc });
            })
            .WithSummary("Invalidate access token")
            .WithDescription("Logs out the current user by blacklisting the presented access token.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }
}
