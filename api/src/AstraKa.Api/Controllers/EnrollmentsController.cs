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
public sealed class EnrollmentsController(ISender sender) : ControllerBase
{
    /// <summary>Enrolls the signed-in Wanderer in a cohort.</summary>
    [HttpPost("cohorts/{cohortId:guid}/enrollments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enroll(Guid cohortId, CancellationToken cancellationToken)
    {
        await sender.Send(new EnrollCommand(cohortId), cancellationToken);
        return CreatedAtAction(nameof(GetMyEnrollmentById), new { cohortId }, null);
    }

    /// <summary>The signed-in Wanderer's own enrollments, paged and filterable by display status.</summary>
    [HttpGet("me/enrollments")]
    [ProducesResponseType<PagedResult<MyEnrollmentDto>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<MyEnrollmentDto>> GetMyEnrollments([FromQuery] GetMyEnrollmentsQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send(query, cancellationToken);
    }

    /// <summary>One of the signed-in Wanderer's own enrollments - 404 if missing or not theirs.</summary>
    [HttpGet("me/enrollments/{cohortId:guid}")]
    [ProducesResponseType<MyEnrollmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<MyEnrollmentDto> GetMyEnrollmentById(Guid cohortId, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetMyEnrollmentByIdQuery(cohortId), cancellationToken);
    }

    /// <summary>Active -> Left. 409 if already left or already completed. The seat is not freed and money is never refunded.</summary>
    [HttpPost("me/enrollments/{cohortId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Leave(Guid cohortId, CancellationToken cancellationToken)
    {
        await sender.Send(new LeaveEnrollmentCommand(cohortId), cancellationToken);
        return NoContent();
    }
}
