using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using AstraKa.Domain.Enums;
using AstraKa.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Me;

/// <summary>
/// The desktop app's onboarding call. Same "one account, one role, no re-onboarding" rule as
/// <see cref="OnboardAsWandererCommand"/> - no FK to validate up front here, since Title is free text.
/// </summary>
public sealed record OnboardAsStarshepherdCommand(string Name, string Title) : IRequest<Guid>;

public sealed class OnboardAsStarshepherdCommandValidator : AbstractValidator<OnboardAsStarshepherdCommand>
{
    public OnboardAsStarshepherdCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
    }
}

public sealed class OnboardAsStarshepherdCommandHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<OnboardAsStarshepherdCommand, Guid>
{
    public async Task<Guid> Handle(OnboardAsStarshepherdCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUser.ExternalId) || string.IsNullOrWhiteSpace(currentUser.Email))
        {
            throw new DomainException("Your account has no email on file with the identity provider.");
        }

        var alreadyOnboarded = await db.Users.AnyAsync(u => u.ExternalId == currentUser.ExternalId, cancellationToken);
        if (alreadyOnboarded)
        {
            throw new DomainException("This account has already onboarded.");
        }

        var user = new User(Guid.NewGuid(), currentUser.ExternalId, currentUser.Email, request.Name, UserRole.Starshepherd);
        var starshepherd = new Starshepherd(user.Id, request.Title);

        db.Users.Add(user);
        db.Starshepherds.Add(starshepherd);
        await db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
