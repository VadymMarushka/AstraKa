using AstraKa.Application.Me;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>
/// Any authenticated caller, regardless of role - onboarding can't require a role the caller
/// doesn't have yet. Thin by design: only forwards to MediatR, per CLAUDE.md.
/// </summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<MeDto> Get(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetMeQuery(), cancellationToken);
    }

    [HttpPost("wanderer")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OnboardAsWanderer([FromBody] OnboardAsWandererCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), null);
    }

    [HttpPost("starshepherd")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OnboardAsStarshepherd([FromBody] OnboardAsStarshepherdCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), null);
    }
}
