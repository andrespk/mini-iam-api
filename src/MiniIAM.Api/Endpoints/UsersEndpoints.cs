using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using MinimalCqrs;
using MiniIAM.Domain.Users.Dtos;
using System.Threading.Tasks;

namespace MiniIAM.Api.Endpoints;

public static class UsersEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/users").RequireAuthorization();

        group.MapPost("/", async (IMediator mediator, AddUser.Command cmd, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Created($"/users/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Error?.Message);
        });

        group.MapPut("/{id:guid}", async (IMediator mediator, Guid id, UpdateUser.Request body, CancellationToken ct) =>
        {
            var cmd = new UpdateUser.Command(id, body.Name, body.Email, body.Password);
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error?.Message);
        });

        group.MapPost("/{id:guid}/roles/{roleId:guid}", async (IMediator mediator, Guid id, Guid roleId, CancellationToken ct) =>
        {
            var cmd = new AddUserRole.Command(id, roleId);
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error?.Message);
        });
    }
}
