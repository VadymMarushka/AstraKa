using AstraKa.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Cohorts;

/// <summary>
/// Cohort details - 404 if missing. Unlike <c>GetCourseByIdQuery</c>, this does not cascade the
/// course's draft-visibility rule: a cohort id is an unguessable GUID, carries no content worth
/// hiding beyond what's already in this DTO, and enrollment already requires a Published course
/// anyway.
/// </summary>
public sealed record GetCohortByIdQuery(Guid Id) : IRequest<CohortDto>;

public sealed class GetCohortByIdQueryHandler(IAppDbContext db, TimeProvider timeProvider) : IRequestHandler<GetCohortByIdQuery, CohortDto>
{
    public async Task<CohortDto> Handle(GetCohortByIdQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var row = await db.Cohorts
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new { Cohort = c, CourseName = c.Course.Name, CourseAuthorId = c.Course.AuthorId, DayCount = c.Course.CourseDays.Count })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            throw new NotFoundException($"Cohort {request.Id} does not exist.");
        }

        var endDate = row.Cohort.GetEndDate(row.DayCount);
        var phase = row.Cohort.GetPhase(today, endDate);

        return new CohortDto(
            row.Cohort.Id,
            row.Cohort.CourseId,
            row.CourseAuthorId,
            row.CourseName,
            row.Cohort.StartDate,
            endDate,
            row.Cohort.SeatLimit,
            row.Cohort.SeatsTaken,
            row.Cohort.SeatLimit - row.Cohort.SeatsTaken,
            phase);
    }
}
