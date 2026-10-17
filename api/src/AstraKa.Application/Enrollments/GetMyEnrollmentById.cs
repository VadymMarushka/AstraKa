using AstraKa.Application.Common;
using AstraKa.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Enrollments;

/// <summary>One of the caller's own enrollments - 404 if missing or not theirs (identical either way).</summary>
public sealed record GetMyEnrollmentByIdQuery(Guid CohortId) : IRequest<MyEnrollmentDto>;

public sealed class GetMyEnrollmentByIdQueryHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<GetMyEnrollmentByIdQuery, MyEnrollmentDto>
{
    public async Task<MyEnrollmentDto> Handle(GetMyEnrollmentByIdQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var row = await db.Enrollments
            .AsNoTracking()
            .Where(e => e.WandererId == currentUser.UserId && e.CohortId == request.CohortId)
            .Select(e => new { Enrollment = e, Cohort = e.Cohort, CourseName = e.Cohort.Course.Name, DayCount = e.Cohort.Course.CourseDays.Count })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            throw new NotFoundException($"You are not enrolled in cohort {request.CohortId}.");
        }

        var endDate = row.Cohort.GetEndDate(row.DayCount);
        var phase = row.Cohort.GetPhase(today, endDate);
        var status = row.Enrollment.IsCompleted(phase)
            ? EnrollmentDisplayStatus.Completed
            : row.Enrollment.Status == EnrollmentStatus.Left
                ? EnrollmentDisplayStatus.Left
                : EnrollmentDisplayStatus.Active;

        return new MyEnrollmentDto(row.Cohort.Id, row.CourseName, row.Cohort.StartDate, endDate, phase, status, row.Enrollment.PaidPrice, row.Enrollment.EnrolledAt);
    }
}
