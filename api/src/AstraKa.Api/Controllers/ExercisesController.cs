using AstraKa.Api.Auth;
using AstraKa.Application.Common;
using AstraKa.Application.Exercises;
using AstraKa.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>
/// The astral exercise catalog. Anyone signed in can list/read; writes require the Starshepherd
/// role (create) or the <c>AuthorOrOracle</c> resource-based policy (update/delete) - a system
/// exercise (no author) can then only ever be edited by an Oracle, since no other caller's id can
/// match a null resource.
/// </summary>
[ApiController]
[Route("api/v1/exercises")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ExercisesController(ISender sender, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Filterable, sortable, paginated catalog listing.</summary>
    /// <response code="200">The matching page of exercises.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<ExerciseDto>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<ExerciseDto>> List([FromQuery] GetExercisesQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send(query, cancellationToken);
    }

    /// <summary>One exercise by id.</summary>
    /// <response code="200">The exercise.</response>
    /// <response code="404">No exercise with this id exists.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ExerciseDto> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetExerciseByIdQuery(id), cancellationToken);
    }

    /// <summary>Creates a new exercise authored by the signed-in Starshepherd.</summary>
    /// <response code="201">The exercise was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is not a Starshepherd.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Authorize(Policy = Policies.Starshepherd)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateExerciseCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    /// <summary>Full update - the exercise's author or any Oracle.</summary>
    /// <response code="204">The exercise was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller is neither the exercise's author nor an Oracle.</response>
    /// <response code="404">No exercise with this id exists.</response>
    /// <response code="409">The update violates a database constraint.</response>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExerciseRequest request, CancellationToken cancellationToken)
    {
        var exercise = await sender.Send(new GetExerciseByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, exercise.AuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new UpdateExerciseCommand(id, request.Name, request.Description, request.Instructions, request.Unit, request.VideoUrl), cancellationToken);
        return NoContent();
    }

    /// <summary>The exercise's author or any Oracle; 409 if it's used in any course program.</summary>
    /// <response code="204">The exercise was deleted.</response>
    /// <response code="403">The caller is neither the exercise's author nor an Oracle.</response>
    /// <response code="404">No exercise with this id exists.</response>
    /// <response code="409">The exercise is still used in a course program.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var exercise = await sender.Send(new GetExerciseByIdQuery(id), cancellationToken);

        var authorization = await authorizationService.AuthorizeAsync(User, exercise.AuthorId, Policies.AuthorOrOracle);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        await sender.Send(new DeleteExerciseCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Body-only - <c>Id</c> comes from the route, never the body.</summary>
public sealed record UpdateExerciseRequest(string Name, string Description, string Instructions, ExerciseUnit Unit, string? VideoUrl);
