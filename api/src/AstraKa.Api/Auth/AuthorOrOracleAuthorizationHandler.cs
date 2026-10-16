using System.Security.Claims;
using AstraKa.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AstraKa.Api.Auth;

/// <summary>
/// The resource is just the entity's author id - a plain <see cref="Guid"/>? - which covers
/// <c>Course.AuthorId</c> (<see cref="Guid"/>, implicitly convertible) and <c>Exercise.AuthorId</c>
/// (<see cref="Guid"/>?, null = system exercise, so no caller ever matches it except Oracle)
/// uniformly, with no Domain change needed. Call sites (future edit/delete endpoints) load the
/// entity first, then <c>AuthorizeAsync(User, entity.AuthorId, "AuthorOrOracle")</c>.
/// </summary>
internal sealed class AuthorOrOracleAuthorizationHandler : AuthorizationHandler<AuthorOrOracleRequirement, Guid?>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorOrOracleRequirement requirement, Guid? resourceAuthorId)
    {
        var role = context.User.FindFirstValue(AstraKaClaimTypes.Role);
        if (role == nameof(UserRole.Oracle))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirstValue(AstraKaClaimTypes.UserId);
        if (resourceAuthorId is { } authorId && Guid.TryParse(userId, out var parsedUserId) && parsedUserId == authorId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
