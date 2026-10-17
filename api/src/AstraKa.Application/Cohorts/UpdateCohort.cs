using AstraKa.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Cohorts;

/// <summary>
/// The id comes from the route, not the body. No author/Oracle check here - that's the Api's
/// resource-based <c>AuthorOrOracle</c> policy, run before this is ever sent.
/// </summary>
public sealed record UpdateCohortCommand(Guid Id, DateOnly StartDate, int SeatLimit) : IRequest;

public sealed class UpdateCohortCommandValidator : AbstractValidator<UpdateCohortCommand>
{
    public UpdateCohortCommandValidator()
    {
        RuleFor(x => x.SeatLimit).GreaterThan(0);
    }
}

public sealed class UpdateCohortCommandHandler(IAppDbContext db, TimeProvider timeProvider) : IRequestHandler<UpdateCohortCommand>
{
    public async Task Handle(UpdateCohortCommand request, CancellationToken cancellationToken)
    {
        var cohort = await db.Cohorts.SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (cohort is null)
        {
            throw new NotFoundException($"Cohort {request.Id} does not exist.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Rejects an already-started cohort, a new StartDate that isn't itself in the future, and
        // a SeatLimit below SeatsTaken, in that order.
        cohort.Update(today, request.StartDate, request.SeatLimit);

        await db.SaveChangesAsync(cancellationToken);
    }
}
