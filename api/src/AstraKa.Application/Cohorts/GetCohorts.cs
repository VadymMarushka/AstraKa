using AstraKa.Application.Common;
using AstraKa.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Cohorts;

/// <summary>
/// Filterable, sortable, paginated listing of a single course's cohorts ("Constellations").
/// Visibility is the caller's responsibility: the controller sends <c>GetCourseByIdQuery</c> first
/// so a draft course not visible to the caller 404s before this ever runs.
/// </summary>
public sealed record GetCohortsQuery(
    Guid CourseId,
    bool UpcomingOnly = false,
    bool HasFreeSeats = false,
    bool Descending = false,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CohortDto>>;

/// <summary>
/// Shared by both the list and the details endpoint - the task gives one DTO shape for both.
/// <see cref="CourseAuthorId"/> isn't part of that shape; it's here for the same reason
/// <c>CourseDetailsDto.AuthorId</c> is - so the controller can authorize PUT/DELETE without a
/// second query.
/// </summary>
public sealed record CohortDto(
    Guid Id,
    Guid CourseId,
    Guid CourseAuthorId,
    string CourseName,
    DateOnly StartDate,
    DateOnly EndDate,
    int SeatLimit,
    int SeatsTaken,
    int SeatsLeft,
    CohortPhase Phase);

public sealed class GetCohortsQueryValidator : AbstractValidator<GetCohortsQuery>
{
    public GetCohortsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class GetCohortsQueryHandler(IAppDbContext db, TimeProvider timeProvider) : IRequestHandler<GetCohortsQuery, PagedResult<CohortDto>>
{
    public async Task<PagedResult<CohortDto>> Handle(GetCohortsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var query = db.Cohorts.AsNoTracking().Where(c => c.CourseId == request.CourseId);

        if (request.UpcomingOnly)
        {
            query = query.Where(c => c.StartDate > today);
        }

        if (request.HasFreeSeats)
        {
            query = query.Where(c => c.SeatsTaken < c.SeatLimit);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.Descending ? query.OrderByDescending(c => c.StartDate) : query.OrderBy(c => c.StartDate);

        // DayCount is projected here so the page's rows, not the whole table, pay for it; the rest
        // (EndDate/Phase) is computed below from the real materialized Cohort, since its public
        // constructor would reset SeatsTaken to 0 and can't be used to rebuild it.
        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new { Cohort = c, CourseName = c.Course.Name, CourseAuthorId = c.Course.AuthorId, DayCount = c.Course.CourseDays.Count })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row =>
        {
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
        }).ToList();

        return new PagedResult<CohortDto>(items, request.Page, request.PageSize, totalCount);
    }
}
