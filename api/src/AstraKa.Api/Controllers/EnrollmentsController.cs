using AstraKa.Api.Auth;
using AstraKa.Application.Common;
using AstraKa.Application.Enrollments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>
/// A Wanderer's enrollments ("joining a Constellation"). Bundled in one controller across two
/// route prefixes (<c>/cohorts/{cohortId}/enrollments</c>, <c>/me/enrollments...</c>), same
/// precedent as <see cref="CohortsController"/> - both prefixes are squarely the Enrollment
/// resource. No resource-based authorization here: every rule is either the <c>Wanderer</c> role
/// policy or "is this the caller's own data", both already expressed via <see cref="ICurrentUser"/>
/// inside each query/command - an enrollment has no author to check against.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize(Policy = Policies.Wanderer)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class EnrollmentsController(ISender sender) : ControllerBase
{
    /// <summary>Enrolls the signed-in Wanderer in a cohort.</summary>
    /// <response code="201">The Wanderer was enrolled.</response>
    /// <response code="404">No cohort with this id exists.</response>
    /// <response code="409">The course isn't published, the cohort has started, there's no free seat, the sign isn't allowed, or the Wanderer is already enrolled.</response>
    [HttpPost("cohorts/{cohortId:guid}/enrollments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll(Guid cohortId, CancellationToken cancellationToken)
    {
        await sender.Send(new EnrollCommand(cohortId), cancellationToken);
        return CreatedAtAction(nameof(GetMyEnrollmentById), new { cohortId }, null);
    }

    /// <summary>The signed-in Wanderer's own enrollments, paged and filterable by display status.</summary>
    /// <response code="200">The matching page of enrollments.</response>
    [HttpGet("me/enrollments")]
    [ProducesResponseType<PagedResult<MyEnrollmentDto>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<MyEnrollmentDto>> GetMyEnrollments([FromQuery] GetMyEnrollmentsQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send(query, cancellationToken);
    }

    /// <summary>One of the signed-in Wanderer's own enrollments - 404 if missing or not theirs.</summary>
    /// <response code="200">The enrollment.</response>
    /// <response code="404">No enrollment in this cohort exists for the signed-in Wanderer.</response>
    [HttpGet("me/enrollments/{cohortId:guid}")]
    [ProducesResponseType<MyEnrollmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<MyEnrollmentDto> GetMyEnrollmentById(Guid cohortId, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetMyEnrollmentByIdQuery(cohortId), cancellationToken);
    }

    /// <summary>Active -> Left. 409 if already left or already completed. The seat is not freed and money is never refunded.</summary>
    /// <response code="204">The Wanderer left the enrollment.</response>
    /// <response code="404">No enrollment in this cohort exists for the signed-in Wanderer.</response>
    /// <response code="409">The enrollment was already left, or is already completed.</response>
    [HttpPost("me/enrollments/{cohortId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Leave(Guid cohortId, CancellationToken cancellationToken)
    {
        await sender.Send(new LeaveEnrollmentCommand(cohortId), cancellationToken);
        return NoContent();
    }
}
