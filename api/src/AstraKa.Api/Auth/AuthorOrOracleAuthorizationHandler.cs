using System.Security.Claims;
using AstraKa.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AstraKa.Api.Auth;

/// <summary>
/// The resource is just the entity's author id - a plain <see cref="Guid"/>? - which covers
/// <c>Course.AuthorId</c> (<see cref="Guid"/>, implicitly convertible) and <c>Exercise.AuthorId</c>
/// (<see cref="Guid"/>?, null = system exercise, so no caller ever matches it except Oracle)
/// uniformly, with no Domain change needed. Call sites load the entity first, then
/// <c>AuthorizeAsync(User, entity.AuthorId, "AuthorOrOracle")</c>.
///
/// Implements the plain <see cref="IAuthorizationHandler"/>, not the generic
/// <c>AuthorizationHandler&lt;TRequirement, TResource&gt;</c> base - boxing a <c>Guid?</c> with no
/// value produces a null reference, not a boxed <c>Nullable&lt;Guid&gt;</c>, so that base class's
/// `context.Resource is TResource` check never matches a null resource and the handler would
/// silently never run for a system exercise. Checking `context.Resource is Guid` by hand (true
/// only when there *is* an author id; false, not thrown, when the resource is null) avoids that.
/// </summary>
internal sealed class AuthorOrOracleAuthorizationHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements.OfType<AuthorOrOracleRequirement>().ToList())
        {
            var role = context.User.FindFirstValue(AstraKaClaimTypes.Role);
            if (role == nameof(UserRole.Oracle))
            {
                context.Succeed(requirement);
                continue;
            }

            if (context.Resource is Guid resourceAuthorId)
            {
                var userId = context.User.FindFirstValue(AstraKaClaimTypes.UserId);
                if (Guid.TryParse(userId, out var parsedUserId) && parsedUserId == resourceAuthorId)
                {
                    context.Succeed(requirement);
                }
            }
        }

        return Task.CompletedTask;
    }
}
