using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Enrollments;

/// <summary>No body - <c>CohortId</c> comes from the route, the Wanderer from <see cref="ICurrentUser"/>.</summary>
public sealed record LeaveEnrollmentCommand(Guid CohortId) : IRequest;

public sealed class LeaveEnrollmentCommandHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<LeaveEnrollmentCommand>
{
    public async Task Handle(LeaveEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await db.Enrollments
            .Include(e => e.Cohort)
            .ThenInclude(c => c.Course)
            .ThenInclude(co => co.CourseDays)
            .SingleOrDefaultAsync(e => e.WandererId == currentUser.UserId && e.CohortId == request.CohortId, cancellationToken);

        if (enrollment is null)
        {
            throw new NotFoundException($"You are not enrolled in cohort {request.CohortId}.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var endDate = enrollment.Cohort.GetEndDate(enrollment.Cohort.Course.CourseDays.Count);
        var phase = enrollment.Cohort.GetPhase(today, endDate);

        // Rejects leaving twice and leaving an already-completed enrollment.
        enrollment.Leave(phase);

        await db.SaveChangesAsync(cancellationToken);
    }
}
