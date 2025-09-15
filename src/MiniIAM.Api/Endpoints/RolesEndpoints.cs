using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using MiniIAM.Domain.Roles.Dtos;
using MinimalCqrs;
using System.Threading.Tasks;

namespace MiniIAM.Api.Endpoints;

public static class RolesEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/roles").RequireAuthorization();

        group.MapPost("/", async (IMediator mediator, AddRole.Command cmd, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Created($"/roles/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Error?.Message);
        });

        group.MapPut("/{id:guid}", async (IMediator mediator, Guid id, UpdateRole.Request body, CancellationToken ct) =>
        {
            var cmd = new UpdateRole.Command(id, body.Name);
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error?.Message);
        });
    }
}
