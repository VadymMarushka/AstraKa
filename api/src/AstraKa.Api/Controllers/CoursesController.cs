using AstraKa.Api.Auth;
using AstraKa.Application.Common;
using AstraKa.Application.Courses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>
/// Course templates (not their program - days/exercises are ADO.NET-owned, a separate feature).
/// Anyone signed in can list/read; writes require the Starshepherd role (create) or the
/// <c>AuthorOrOracle</c> resource-based policy (update/publish/delete).
/// </summary>
[ApiController]
[Route("api/v1/courses")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CoursesController(ISender sender, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Filterable, sortable, paginated course listing.</summary>
    /// <response code="200">The matching page of courses.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<CourseListItemDto>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<CourseListItemDto>> List([FromQuery] GetCoursesQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send(query, cancellationToken);
    }

    /// <summary>Course details. A draft is a 404 for anyone except its author or Oracle.</summary>
    /// <response code="200">The course.</response>
    /// <response code="404">No course with this id exists (or it's a draft hidden from the caller).</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CourseDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<CourseDetailsDto> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetCourseByIdQuery(id), cancellationToken);
    }

    /// <summary>Creates a new course as a Draft, authored by the signed-in Starshepherd.</summary>
    /// <response code="201">The course was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is not a Starshepherd.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Authorize(Policy = Policies.Starshepherd)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    /// <summary>Full update, including replacing the sign set - the course's author or any Oracle.</summary>
    /// <response code="204">The course was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No course with this id exists (or it's a draft hidden from the caller).</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = await sender.Send(new GetCourseByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, course.AuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new UpdateCourseCommand(id, request.Name, request.Description, request.Promise, request.Price, request.ZodiacSignIds), cancellationToken);
        return NoContent();
    }

    /// <summary>Draft -> Published - the course's author or any Oracle; 409 if it has no days or is already published.</summary>
    /// <response code="204">The course was published.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No course with this id exists (or it's a draft hidden from the caller).</response>
    /// <response code="409">The course has no days, or is already published.</response>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var course = await sender.Send(new GetCourseByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, course.AuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new PublishCourseCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>The course's author or any Oracle; 409 if it has any cohorts.</summary>
    /// <response code="204">The course was deleted.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No course with this id exists (or it's a draft hidden from the caller).</response>
    /// <response code="409">The course has cohorts and cannot be deleted.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var course = await sender.Send(new GetCourseByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, course.AuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new DeleteCourseCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Body-only - <c>Id</c> comes from the route, never the body.</summary>
public sealed record UpdateCourseRequest(string Name, string Description, string Promise, decimal Price, IReadOnlyList<int> ZodiacSignIds);
