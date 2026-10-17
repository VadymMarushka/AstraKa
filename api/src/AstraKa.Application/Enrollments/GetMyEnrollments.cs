using AstraKa.Application.Common;
using AstraKa.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Enrollments;

/// <summary>The caller's own enrollments, filterable by the derived display status.</summary>
public sealed record GetMyEnrollmentsQuery(EnrollmentDisplayStatus? Status = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<MyEnrollmentDto>>;

/// <summary><see cref="EnrollmentStatus"/> only distinguishes Active/Left in storage - this adds
/// the computed "Completed" (Active + the cohort has finished) for API consumers.</summary>
public enum EnrollmentDisplayStatus
{
    Active,
    Completed,
    Left,
}

public sealed record MyEnrollmentDto(
    Guid CohortId,
    string CourseName,
    DateOnly StartDate,
    DateOnly EndDate,
    CohortPhase Phase,
    EnrollmentDisplayStatus Status,
    decimal PaidPrice,
    DateTime EnrolledAt);

public sealed class GetMyEnrollmentsQueryValidator : AbstractValidator<GetMyEnrollmentsQuery>
{
    public GetMyEnrollmentsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class GetMyEnrollmentsQueryHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<GetMyEnrollmentsQuery, PagedResult<MyEnrollmentDto>>
{
    public async Task<PagedResult<MyEnrollmentDto>> Handle(GetMyEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // DisplayStatus depends on Cohort.GetPhase (StartDate + Course.CourseDays.Count + today),
        // not a simple SQL-translatable predicate. One Wanderer's own enrollments are always few,
        // so this fetches them once and computes/filters/pages in memory - clear and correct beats
        // a clever EF-translatable date expression here.
        var rows = await db.Enrollments
            .AsNoTracking()
            .Where(e => e.WandererId == currentUser.UserId)
            .Select(e => new { Enrollment = e, Cohort = e.Cohort, CourseName = e.Cohort.Course.Name, DayCount = e.Cohort.Course.CourseDays.Count })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row =>
        {
            var endDate = row.Cohort.GetEndDate(row.DayCount);
            var phase = row.Cohort.GetPhase(today, endDate);
            var status = row.Enrollment.IsCompleted(phase)
                ? EnrollmentDisplayStatus.Completed
                : row.Enrollment.Status == EnrollmentStatus.Left
                    ? EnrollmentDisplayStatus.Left
                    : EnrollmentDisplayStatus.Active;

            return new MyEnrollmentDto(row.Cohort.Id, row.CourseName, row.Cohort.StartDate, endDate, phase, status, row.Enrollment.PaidPrice, row.Enrollment.EnrolledAt);
        }).ToList();

        if (request.Status is { } status)
        {
            items = items.Where(i => i.Status == status).ToList();
        }

        var totalCount = items.Count;
        var page = items.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

        return new PagedResult<MyEnrollmentDto>(page, request.Page, request.PageSize, totalCount);
    }
}
