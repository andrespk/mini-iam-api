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
    public sealed record AddUserRoleRequest(Guid UserId, IList<RoleDto> Roles, Guid ByUserId);
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
                var response=await commands.DispatchAsync<AddUser.Command, Guid>(command, ct);
                
                return Results.Created($"/users/{response}", new { Id = response });
            })
            .WithSummary("Create a new user")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
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

                await commands.DispatchAsync(new AddUserUser.Command(request.UserId, request.Roles, byUserId), ct);

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
