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
public sealed class CoursesController(ISender sender, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Filterable, sortable, paginated course listing.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<CourseListItemDto>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<CourseListItemDto>> List([FromQuery] GetCoursesQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send(query, cancellationToken);
    }

    /// <summary>Course details. A draft is a 404 for anyone except its author or Oracle.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CourseDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<CourseDetailsDto> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetCourseByIdQuery(id), cancellationToken);
    }

    /// <summary>Creates a new course as a Draft, authored by the signed-in Starshepherd.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.Starshepherd)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    /// <summary>Full update, including replacing the sign set - the course's author or any Oracle.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
