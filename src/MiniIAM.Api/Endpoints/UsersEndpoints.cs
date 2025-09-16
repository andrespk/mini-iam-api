using System.Security.Claims;
using Asp.Versioning;
using MiniIAM.Application.UseCases.Users;
using MiniIAM.Domain.Roles.Dtos;
using MiniIAM.Infrastructure.Cqrs.Abstractions;
using MiniIAM.Infrastructure.Caching.Abstractions;

namespace MiniIAM.Endpoints;

public static class UsersEndpoints
{
    public const string SubClaimType = "sub";
    
    // Cache keys
    private const string UserCacheKeyPrefix = "user:";
    private const string UserRolesCacheKeyPrefix = "user:roles:";
    
    // Cache TTL
    private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan UserRolesCacheTtl = TimeSpan.FromMinutes(10);

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
                ICachingService cacheService,
                AddUserRequest request,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(user, out var byUserId))
                    return Results.Unauthorized();

                var command = new AddUser.Command(request.Email, request.Name, request.Password, byUserId);
                var response = await commands.DispatchAsync<AddUser.Command, Guid>(command, ct);

                // Invalidate cache for the new user (in case it was cached before)
                await InvalidateUserCacheAsync(cacheService, response);

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
                ICachingService cacheService,
                CancellationToken ct) =>
            {
                var cacheKey = $"{UserCacheKeyPrefix}{id}";
                
                // Try to get from cache first
                var cachedUser = await cacheService.GetAsync<MiniIAM.Domain.Users.Dtos.UserDto>(cacheKey);
                if (cachedUser != null)
                {
                    return Results.Ok(cachedUser);
                }

                // If not in cache, get from database
                var command = new GetUser.Command(id);
                var response =
                    await commands.DispatchAsync<GetUser.Command, MiniIAM.Domain.Users.Dtos.UserDto>(command, ct);

                if (response == null)
                    return Results.NotFound();

                // Cache the result
                await cacheService.SetAsync(cacheKey, response, UserCacheTtl);

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
                ICachingService cacheService,
                AddUserRoleRequest request,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(user, out var byUserId))
                    return Results.Unauthorized();

                await commands.DispatchAsync(
                    new AddUserRole.Command(request.UserId, request.RolesIds.Select(x => new RoleDto(x)).ToList(),
                        byUserId), ct);

                // Invalidate user cache since roles have been updated
                await InvalidateUserCacheAsync(cacheService, request.UserId);

                return Results.Created();
            })
            .WithSummary("Add one or more roles to user")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}/roles", async (
                Guid id,
                ICommandDispatcher commands,
                ICachingService cacheService,
                CancellationToken ct) =>
            {
                var cacheKey = $"{UserRolesCacheKeyPrefix}{id}";
                
                // Try to get from cache first
                var cachedRoles = await cacheService.GetAsync<IList<RoleDto>>(cacheKey);
                if (cachedRoles != null)
                {
                    return Results.Ok(cachedRoles);
                }

                // If not in cache, get from database
                // Note: This would require a new use case GetUserRoles
                // For now, we'll return a placeholder response
                var roles = new List<RoleDto>(); // This should be replaced with actual implementation
                
                // Cache the result
                await cacheService.SetAsync(cacheKey, roles, UserRolesCacheTtl);

                return Results.Ok(roles);
            })
            .WithSummary("Get user roles by user ID")
            .Produces<IList<RoleDto>>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid id)
    {
        id = Guid.Empty;
        return Guid.TryParse(user.FindFirst(SubClaimType)?.Value, out id);
    }

    private static async Task InvalidateUserCacheAsync(ICachingService cacheService, Guid userId)
    {
        var userCacheKey = $"{UserCacheKeyPrefix}{userId}";
        var userRolesCacheKey = $"{UserRolesCacheKeyPrefix}{userId}";
        
        // Remove user cache
        await cacheService.RemoveAsync(userCacheKey);
        
        // Remove user roles cache
        await cacheService.RemoveAsync(userRolesCacheKey);
    }
}