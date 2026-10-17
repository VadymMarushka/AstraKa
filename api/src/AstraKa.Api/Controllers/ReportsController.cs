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
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ReportsController(ISender sender, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Multi-table JOIN of a cohort's participants, filterable by status and sortable - the course's author or any Oracle.</summary>
    /// <param name="cohortId">The cohort to report on.</param>
    /// <param name="status">Optional filter - only participants with this enrollment status.</param>
    /// <param name="sortBy">Which column to sort by.</param>
    /// <param name="descending">Sort descending instead of ascending.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The matching participants.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No cohort with this id exists.</response>
    [HttpGet("cohorts/{cohortId:guid}/participants")]
    [ProducesResponseType<IReadOnlyList<CohortParticipantRecord>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
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
    /// <response code="200">The course's statistics.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No course with this id exists (or it's a draft hidden from the caller).</response>
    [HttpGet("courses/{courseId:guid}/statistics")]
    [ProducesResponseType<CourseStatisticsRecord>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
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
    /// <response code="200">The cohort's harmony trend, one point per calendar date.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No cohort with this id exists.</response>
    [HttpGet("cohorts/{cohortId:guid}/harmony-trend")]
    [ProducesResponseType<IReadOnlyList<HarmonyTrendPointDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
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
