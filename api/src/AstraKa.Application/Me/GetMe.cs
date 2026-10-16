using AstraKa.Application.Common;
using AstraKa.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Me;

/// <summary>
/// The signed-in caller's own profile. A signed-in user who hasn't onboarded yet (no matching
/// <c>User</c> row) gets a 404 from this - that's how clients know to show the onboarding form.
/// </summary>
public sealed record GetMeQuery : IRequest<MeDto>;

/// <summary>
/// One flat shape for every role - <see cref="ZodiacSignId"/> is set only for a Wanderer,
/// <see cref="Title"/> only for a Starshepherd - rather than a subtype per role, same reasoning as
/// <see cref="Domain.Exceptions.DomainException"/>: one simple shape is easier to explain.
/// </summary>
public sealed record MeDto(Guid UserId, string Name, string Email, UserRole Role, int? ZodiacSignId, string? Title);

public sealed class GetMeQueryHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<GetMeQuery, MeDto>
{
    public async Task<MeDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userId)
        {
            throw new NotFoundException("You have not onboarded yet.");
        }

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Wanderer)
            .Include(u => u.Starshepherd)
            .SingleAsync(u => u.Id == userId, cancellationToken);

        return new MeDto(user.Id, user.Name, user.Email, user.Role, user.Wanderer?.ZodiacSignId, user.Starshepherd?.Title);
    }
}
