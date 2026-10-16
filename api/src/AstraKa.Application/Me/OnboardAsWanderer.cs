using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using AstraKa.Domain.Enums;
using AstraKa.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Me;

/// <summary>
/// The web app's onboarding call. The role is decided by *this endpoint*, not chosen by the
/// caller - one account can only ever onboard once, into one role (enforced below, not by a
/// database constraint, so the error is a clear 409 rather than a raw uniqueness violation).
/// </summary>
public sealed record OnboardAsWandererCommand(string Name, int ZodiacSignId) : IRequest<Guid>;

public sealed class OnboardAsWandererCommandValidator : AbstractValidator<OnboardAsWandererCommand>
{
    public OnboardAsWandererCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ZodiacSignId).InclusiveBetween(1, 12);
    }
}

public sealed class OnboardAsWandererCommandHandler(IAppDbContext db, ICurrentUser currentUser) : IRequestHandler<OnboardAsWandererCommand, Guid>
{
    public async Task<Guid> Handle(OnboardAsWandererCommand request, CancellationToken cancellationToken)
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

        var zodiacSignExists = await db.ZodiacSigns.AnyAsync(z => z.Id == request.ZodiacSignId, cancellationToken);
        if (!zodiacSignExists)
        {
            throw new NotFoundException($"Zodiac sign {request.ZodiacSignId} does not exist.");
        }

        var user = new User(Guid.NewGuid(), currentUser.ExternalId, currentUser.Email, request.Name, UserRole.Wanderer);
        var wanderer = new Wanderer(user.Id, request.ZodiacSignId);

        db.Users.Add(user);
        db.Wanderers.Add(wanderer);
        await db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
