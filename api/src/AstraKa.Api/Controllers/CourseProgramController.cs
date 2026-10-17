using AstraKa.Api.Auth;
using AstraKa.Application.CourseProgram;
using AstraKa.Application.Courses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>
/// A course's base program (days + day exercises) - ADO.NET-owned, per CLAUDE.md's "ADO.NET vs EF
/// Core boundary". Revelations (per-enrollment extra exercises) are out of scope here. Bundled in
/// one controller across two route prefixes (<c>/courses/{courseId}/days</c>, <c>/days/{dayId}</c>,
/// <c>/days/{dayId}/exercises</c>, <c>/day-exercises/{id}</c>, <c>/courses/with-program</c>), same
/// precedent as <see cref="CohortsController"/> - all squarely the "course program" resource.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CourseProgramController(ISender sender, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>A course's full base program. The course's visibility rule applies (a draft is a 404 for anyone but its author or Oracle).</summary>
    /// <response code="200">The course's days and their base-program exercises.</response>
    /// <response code="404">No course with this id exists (or it's a draft hidden from the caller).</response>
    [HttpGet("courses/{courseId:guid}/days")]
    [ProducesResponseType<IReadOnlyList<CourseDayWithExercisesRecord>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<CourseDayWithExercisesRecord>> GetDays(Guid courseId, CancellationToken cancellationToken)
    {
        await sender.Send(new GetCourseByIdQuery(courseId), cancellationToken);

        return await sender.Send(new GetCourseDaysQuery(courseId), cancellationToken);
    }

    /// <summary>Adds a day to a course's program - the course's author or any Oracle. 409 if the day number already exists.</summary>
    /// <response code="201">The day was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No course with this id exists (or it's a draft hidden from the caller).</response>
    /// <response code="409">A day with this day number already exists for the course.</response>
    [HttpPost("courses/{courseId:guid}/days")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDay(Guid courseId, [FromBody] CreateDayRequest request, CancellationToken cancellationToken)
    {
        var course = await sender.Send(new GetCourseByIdQuery(courseId), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, course.AuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        var id = await sender.Send(new CreateDayCommand(courseId, request.DayNumber, request.Title, request.Description, request.MeetLink), cancellationToken);
        return Created($"/api/v1/days/{id}", null);
    }

    /// <summary>Full update of a day's mutable fields - the course's author or any Oracle.</summary>
    /// <response code="204">The day was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No day with this id exists.</response>
    [HttpPut("days/{dayId:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDay(Guid dayId, [FromBody] UpdateDayRequest request, CancellationToken cancellationToken)
    {
        var day = await sender.Send(new GetCourseDayByIdQuery(dayId), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, day.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new UpdateDayCommand(dayId, request.Title, request.Description, request.MeetLink), cancellationToken);
        return NoContent();
    }

    /// <summary>The course's author or any Oracle; 409 if the day's exercises have been marked completed.</summary>
    /// <response code="204">The day was deleted.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No day with this id exists.</response>
    /// <response code="409">The day's exercises have been marked completed and it cannot be deleted.</response>
    [HttpDelete("days/{dayId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDay(Guid dayId, CancellationToken cancellationToken)
    {
        var day = await sender.Send(new GetCourseDayByIdQuery(dayId), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, day.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new DeleteDayCommand(dayId), cancellationToken);
        return NoContent();
    }

    /// <summary>Adds an exercise to a day's base program - the course's author or any Oracle.</summary>
    /// <response code="201">The day exercise was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No day with this id exists.</response>
    /// <response code="409">The exercise id doesn't exist.</response>
    [HttpPost("days/{dayId:guid}/exercises")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDayExercise(Guid dayId, [FromBody] CreateDayExerciseRequest request, CancellationToken cancellationToken)
    {
        var day = await sender.Send(new GetCourseDayByIdQuery(dayId), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, day.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        var id = await sender.Send(new CreateDayExerciseCommand(dayId, request.ExerciseId, request.OrderNumber, request.Sets, request.Amount), cancellationToken);
        return Created($"/api/v1/day-exercises/{id}", null);
    }

    /// <summary>Full update of a day exercise's mutable fields - the course's author or any Oracle.</summary>
    /// <response code="204">The day exercise was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No day exercise with this id exists.</response>
    [HttpPut("day-exercises/{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDayExercise(Guid id, [FromBody] UpdateDayExerciseRequest request, CancellationToken cancellationToken)
    {
        var dayExercise = await sender.Send(new GetDayExerciseByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, dayExercise.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new UpdateDayExerciseCommand(id, request.OrderNumber, request.Sets, request.Amount), cancellationToken);
        return NoContent();
    }

    /// <summary>The course's author or any Oracle; 409 if the exercise has been marked completed.</summary>
    /// <response code="204">The day exercise was deleted.</response>
    /// <response code="403">The caller is neither the course's author nor an Oracle.</response>
    /// <response code="404">No day exercise with this id exists.</response>
    /// <response code="409">The exercise has been marked completed and it cannot be deleted.</response>
    [HttpDelete("day-exercises/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDayExercise(Guid id, CancellationToken cancellationToken)
    {
        var dayExercise = await sender.Send(new GetDayExerciseByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, dayExercise.CourseAuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new DeleteDayExerciseCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Creates a draft course with its full program (days + day exercises) in one ADO.NET
    /// transaction - any Starshepherd, no existing course to check authorship against. A bad
    /// exercise id anywhere in the program rolls everything back - the lab 2 commit/rollback
    /// scenario, shown through the API.
    /// </summary>
    /// <response code="201">The course and its full program were created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is not a Starshepherd.</response>
    /// <response code="409">An exercise id in the program doesn't exist - nothing was persisted.</response>
    [HttpPost("courses/with-program")]
    [Consumes("application/json")]
    [Authorize(Policy = Policies.Starshepherd)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCourseWithProgram([FromBody] CreateCourseWithProgramCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return Created($"/api/v1/courses/{id}", null);
    }
}

/// <summary>Body-only - <c>CourseId</c> comes from the route, never the body.</summary>
public sealed record CreateDayRequest(int DayNumber, string Title, string? Description, string? MeetLink);

/// <summary>Body-only - <c>Id</c> comes from the route, never the body.</summary>
public sealed record UpdateDayRequest(string Title, string? Description, string? MeetLink);

/// <summary>Body-only - <c>DayId</c> comes from the route, never the body.</summary>
public sealed record CreateDayExerciseRequest(Guid ExerciseId, int OrderNumber, int Sets, int Amount);

/// <summary>Body-only - <c>Id</c> comes from the route, never the body.</summary>
public sealed record UpdateDayExerciseRequest(int OrderNumber, int Sets, int Amount);
