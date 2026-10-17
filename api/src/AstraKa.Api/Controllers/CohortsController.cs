using AstraKa.Api.Auth;
using AstraKa.Application.Cohorts;
using AstraKa.Application.Common;
using AstraKa.Application.Courses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>
/// Cohorts ("Constellations") - runs of a course. List/details routes sit under
/// <c>/api/v1/courses/{courseId}/cohorts</c>; everything addressed by its own id sits under
/// <c>/api/v1/cohorts/{id}</c>. Writes require the <c>AuthorOrOracle</c> resource-based policy,
/// checked against the owning course's author - a cohort has no author of its own.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class CohortsController(ISender sender, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Filterable, sortable, paginated cohort listing. The course's visibility rule applies (a draft is a 404 for anyone but its author or Oracle).</summary>
    [HttpGet("courses/{courseId:guid}/cohorts")]
    [ProducesResponseType<PagedResult<CohortDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PagedResult<CohortDto>> List(Guid courseId, [FromQuery] GetCohortsRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new GetCourseByIdQuery(courseId), cancellationToken);

        return await sender.Send(
            new GetCohortsQuery(courseId, request.UpcomingOnly, request.HasFreeSeats, request.Descending, request.Page, request.PageSize),
            cancellationToken);
    }

    /// <summary>Cohort details.</summary>
    [HttpGet("cohorts/{id:guid}")]
    [ProducesResponseType<CohortDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<CohortDto> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetCohortByIdQuery(id), cancellationToken);
    }

    /// <summary>Creates a new cohort under a course - the course's author or any Oracle.</summary>
    [HttpPost("courses/{courseId:guid}/cohorts")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid courseId, [FromBody] CreateCohortRequest request, CancellationToken cancellationToken)
    {
        var course = await sender.Send(new GetCourseByIdQuery(courseId), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, course.AuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        var id = await sender.Send(new CreateCohortCommand(courseId, request.StartDate, request.SeatLimit), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    /// <summary>Full update - the course's author or any Oracle; 409 if the cohort has started, the new StartDate isn't in the future, or SeatLimit would drop below seats already taken.</summary>
    [HttpPut("cohorts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCohortRequest request, CancellationToken cancellationToken)
    {
        var cohort = await sender.Send(new GetCohortByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, cohort.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new UpdateCohortCommand(id, request.StartDate, request.SeatLimit), cancellationToken);
        return NoContent();
    }

    /// <summary>The course's author or any Oracle; 409 if the cohort has any enrollments.</summary>
    [HttpDelete("cohorts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var cohort = await sender.Send(new GetCohortByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, cohort.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new DeleteCohortCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Body-only - <c>CourseId</c> comes from the route, never the body.</summary>
public sealed record GetCohortsRequest(bool UpcomingOnly = false, bool HasFreeSeats = false, bool Descending = false, int Page = 1, int PageSize = 20);

/// <summary>Body-only - <c>CourseId</c> comes from the route, never the body.</summary>
public sealed record CreateCohortRequest(DateOnly StartDate, int SeatLimit);

/// <summary>Body-only - <c>Id</c> comes from the route, never the body.</summary>
public sealed record UpdateCohortRequest(DateOnly StartDate, int SeatLimit);
