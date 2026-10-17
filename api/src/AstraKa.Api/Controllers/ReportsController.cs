using AstraKa.Api.Auth;
using AstraKa.Application.Cohorts;
using AstraKa.Application.Courses;
using AstraKa.Application.Reports;
using AstraKa.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>
/// Reports over a course's cohorts/enrollments - two ADO.NET reports (a multi-table JOIN and a
/// GROUP BY statistics summary) plus one EF Core LINQ GroupBy report. All require the
/// <c>AuthorOrOracle</c> resource-based policy against the owning course's author.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class ReportsController(ISender sender, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Multi-table JOIN of a cohort's participants, filterable by status and sortable - the course's author or any Oracle.</summary>
    [HttpGet("cohorts/{cohortId:guid}/participants")]
    [ProducesResponseType<IReadOnlyList<CohortParticipantRecord>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParticipants(
        Guid cohortId, [FromQuery] EnrollmentStatus? status, [FromQuery] CohortParticipantSort sortBy = CohortParticipantSort.Name, [FromQuery] bool descending = false, CancellationToken cancellationToken = default)
    {
        var cohort = await sender.Send(new GetCohortByIdQuery(cohortId), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, cohort.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        var participants = await sender.Send(new GetCohortParticipantsQuery(cohortId, status, sortBy, descending), cancellationToken);
        return Ok(participants);
    }

    /// <summary>GROUP BY statistics for one course (enrollment count, revenue, average harmony) - the course's author or any Oracle.</summary>
    [HttpGet("courses/{courseId:guid}/statistics")]
    [ProducesResponseType<CourseStatisticsRecord>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatistics(Guid courseId, CancellationToken cancellationToken)
    {
        var course = await sender.Send(new GetCourseByIdQuery(courseId), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, course.AuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        var statistics = await sender.Send(new GetCourseStatisticsQuery(courseId), cancellationToken);
        return Ok(statistics);
    }

    /// <summary>EF Core LINQ GroupBy: per calendar date of the cohort, average harmony level, report count, completed exercise count - the course's author or any Oracle.</summary>
    [HttpGet("cohorts/{cohortId:guid}/harmony-trend")]
    [ProducesResponseType<IReadOnlyList<HarmonyTrendPointDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHarmonyTrend(Guid cohortId, CancellationToken cancellationToken)
    {
        var cohort = await sender.Send(new GetCohortByIdQuery(cohortId), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, cohort.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        var trend = await sender.Send(new GetHarmonyTrendQuery(cohortId), cancellationToken);
        return Ok(trend);
    }
}
