using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using FluentValidation;
using MediatR;

namespace AstraKa.Application.Cohorts;

/// <summary>No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record CreateCohortCommand(Guid CourseId, DateOnly StartDate, int SeatLimit) : IRequest<Guid>;

public sealed class CreateCohortCommandValidator : AbstractValidator<CreateCohortCommand>
{
    public CreateCohortCommandValidator()
    {
        RuleFor(x => x.SeatLimit).GreaterThan(0);
    }
}

public sealed class CreateCohortCommandHandler(IAppDbContext db, TimeProvider timeProvider) : IRequestHandler<CreateCohortCommand, Guid>
{
    public async Task<Guid> Handle(CreateCohortCommand request, CancellationToken cancellationToken)
    {
        // No course-existence check here: the controller's GetCourseByIdQuery call already
        // guarantees the course exists and is visible before this is ever sent, and a dangling
        // CourseId would hit GlobalExceptionHandler's FK-violation safety net anyway.
        var cohort = new Cohort(Guid.NewGuid(), request.CourseId, request.StartDate, request.SeatLimit);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        cohort.EnsureScheduledInFuture(today, cohort.StartDate);

        db.Cohorts.Add(cohort);
        await db.SaveChangesAsync(cancellationToken);

        return cohort.Id;
    }
}
