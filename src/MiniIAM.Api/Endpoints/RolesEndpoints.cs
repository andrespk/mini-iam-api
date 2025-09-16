using Asp.Versioning;

namespace Movies.Endpoints;

public static class RolesEndpoints
{
    public static void Map(WebApplication app)
    {
        var v1 = app.NewApiVersionSet().HasApiVersion(new ApiVersion(1,0)).ReportApiVersions().Build();
        var group = app.MapGroup("/roles").WithApiVersionSet(v1).MapToApiVersion(1.0).RequireAuthorization();

        group.MapPost("/",.WithSummary("Create role").WithDescription("Creates a new role.").Produces(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status401Unauthorized) async (ICommandDispatcher commands, AddRole.Command cmd, CancellationToken ct) =>
        {
            var result = await commands.Dispatch(cmd, ct);
            return result.IsSuccess ? Results.Created($"/roles/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Error?.Message);
        });

        group.MapPut("/{id:guid}", async (ICommandDispatcher commands, Guid id, UpdateRole.Request body, CancellationToken ct) =>
        {
            var cmd = new UpdateRole.Command(id, body.Name);
            var result = await commands.Dispatch(cmd, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error?.Message);
        });
    }
}
