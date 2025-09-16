using System.Security.Claims;
using Asp.Versioning;
using MiniIAM.Application.UseCases.Users;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Infrastructure.Cqrs.Abstractions;

namespace MiniIAM.Endpoints;

public static class UsersEndpoints
{
    public const string SubClaimType = "sub";

    public sealed record AddUserRequest(string Email, string Name, string Password, Guid ByUserId);

    public sealed record AddUserRoleRequest(Guid UserId, IList<Guid> RolesIds, Guid ByUserId);

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
                AddUserRequest request,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(user, out var byUserId))
                    return Results.Unauthorized();

                var command = new AddUser.Command(request.Email, request.Name, request.Password, byUserId);
                var response = await commands.DispatchAsync<AddUser.Command, Guid>(command, ct);

                return Results.Created($"/users/{response}", new { Id = response });
            })
            .WithSummary("Create a new user")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}", async (
                Guid id,
                ICommandDispatcher commands,
                CancellationToken ct) =>
            {
                var command = new GetUser.Command(id);
                var response =
                    await commands.DispatchAsync<GetUser.Command, MiniIAM.Domain.Users.Dtos.UserDto>(command, ct);

                if (response == null)
                    return Results.NotFound();

                return Results.Ok(response);
            })
            .WithSummary("Get user by ID")
            .Produces<MiniIAM.Domain.Users.Dtos.UserDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id:guid}/roles", async (
                ClaimsPrincipal user,
                Guid id,
                ICommandDispatcher commands,
                AddUserRoleRequest request,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(user, out var byUserId))
                    return Results.Unauthorized();

                await commands.DispatchAsync(
                    new AddUserRole.Command(request.UserId, request.RolesIds.Select(x => new RoleDto(x)).ToList(),
                        byUserId), ct);

                return Results.Created();
            })
            .WithSummary("Add one or more roles to user")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid id)
    {
        id = Guid.Empty;
        return Guid.TryParse(user.FindFirst(SubClaimType)?.Value, out id);
    }
}