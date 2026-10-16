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
public sealed class ExercisesController(ISender sender, IAuthorizationService authorizationService) : ControllerBase
{
    /// <summary>Filterable, sortable, paginated catalog listing.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ExerciseDto>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<ExerciseDto>> List([FromQuery] GetExercisesQuery query, CancellationToken cancellationToken)
    {
        return await sender.Send(query, cancellationToken);
    }

    /// <summary>One exercise by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ExerciseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ExerciseDto> GetById(Guid id, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetExerciseByIdQuery(id), cancellationToken);
    }

    /// <summary>Creates a new exercise authored by the signed-in Starshepherd.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.Starshepherd)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateExerciseCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    /// <summary>Full update - the exercise's author or any Oracle.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
