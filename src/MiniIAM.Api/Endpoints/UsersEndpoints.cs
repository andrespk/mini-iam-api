// ===============================
// File: src/MiniIAM.Api/Endpoints/UsersEndpoints.cs
// ===============================

using System.Security.Claims;
using Asp.Versioning;
using MiniIAM.Application.UseCases.Users;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Infrastructure.Cqrs.Abstractions;

namespace Movies.Endpoints;

public static class UsersEndpoints
{
    public sealed record CreateUserRequest(string Email, string Name, string Password);
    public sealed record UpdateUserRequest(string Name, IEnumerable<RoleDto> Roles);

    public static void Map(WebApplication app)
    {
        var v1 = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/users")
            .WithApiVersionSet(v1)
            .MapToApiVersion(1.0)
            .WithTags("Users")
            .RequireAuthorization();

        group.MapPost("/", async (
                ClaimsPrincipal user,
                ICommandDispatcher commands,
                CreateUserRequest request,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(user, out var byUserId))
                    return Results.Unauthorized();

                var result = await commands.Dispatch(new AddUser.Command(request.Email, request.Name, request.Password, byUserId), ct);

                if (!result.IsSuccess)
                    return Results.BadRequest();

                var id = result.Value;
                return Results.Created($"/users/{id}", new { id });
            })
            .WithSummary("Create a new user")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", async (
                ClaimsPrincipal user,
                Guid id,
                ICommandDispatcher commands,
                UpdateUserRequest request,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(user, out var byUserId))
                    return Results.Unauthorized();

                var result = await commands.Dispatch(new UpdateUser.Command(request.Name, request.Roles, byUserId), ct);

                if (!result.IsSuccess)
                    return Results.BadRequest();

                return Results.Ok(new { id = result.Value });
            })
            .WithSummary("Update user")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid id)
    {
        id = Guid.Empty;
        var sub = user.FindFirst("sub")?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out id);
    }
}
