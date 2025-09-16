using Asp.Versioning;
using MiniIAM.Application.UseCases.Users;

namespace Movies.Endpoints;

public static class UsersEndpoints
{
    public static void Map(WebApplication app)
    {
        var v1 = app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).ReportApiVersions().Build();
        var group = app.MapGroup("/users").WithApiVersionSet(v1).MapToApiVersion(1.0).RequireAuthorization();

        group.MapPost("/", .WithSummary("Create user").WithDescription("Creates a new user.")
            .Produces(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized) async (ICommandDispatcher commands, AddUser.Command cmd,
                CancellationToken ct) =>
            {
                var result = await commands.Dispatch(cmd, ct);
                return result.IsSuccess
                    ? Results.Created($"/users/{result.Value!.Id}", result.Value)
                    : Results.BadRequest(result.Error?.Message);
            });

        group.MapPut("/{id:guid}",
            async (ICommandDispatcher commands, Guid id, UpdateUser.Request body, CancellationToken ct) =>
            {
                var cmd = new UpdateUser.Command(body.Name, body.Roles, id);
                var result = await commands.Dispatch(cmd, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error?.Message);
            });

        group.MapPost("/{id:guid}/roles/{roleId:guid}",
            async (ICommandDispatcher commands, Guid id, Guid roleId, CancellationToken ct) =>
            {
                var cmd = new AssignRoleToUser.Command(id, roleId);
                var result = await commands.Dispatch(cmd, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error?.Message);
            });
    }
}