using AstraKa.Api.Auth;
using AstraKa.Application.Horoscopes;
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
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class MeController(ISender sender) : ControllerBase
{
    /// <summary>The signed-in caller's own profile. 404 if the account hasn't onboarded yet - that's how clients know to show the onboarding form.</summary>
    /// <response code="200">The caller's profile.</response>
    /// <response code="404">This account hasn't onboarded yet.</response>
    [HttpGet]
    [ProducesResponseType<MeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<MeDto> Get(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetMeQuery(), cancellationToken);
    }

    /// <summary>Onboards the signed-in account as a Wanderer. One account can only ever onboard once, into one role.</summary>
    /// <response code="201">The account was onboarded.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">This account has already onboarded.</response>
    [HttpPost("wanderer")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OnboardAsWanderer([FromBody] OnboardAsWandererCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), null);
    }

    /// <summary>Onboards the signed-in account as a Starshepherd. One account can only ever onboard once, into one role.</summary>
    /// <response code="201">The account was onboarded.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">This account has already onboarded.</response>
    [HttpPost("starshepherd")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OnboardAsStarshepherd([FromBody] OnboardAsStarshepherdCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), null);
    }

    /// <summary>Today's horoscope for the signed-in Wanderer's own sign.</summary>
    /// <response code="200">Today's horoscope.</response>
    /// <response code="403">The caller is not an onboarded Wanderer.</response>
    [HttpGet("horoscope")]
    [Authorize(Policy = Policies.Wanderer)]
    [ProducesResponseType<HoroscopeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<HoroscopeDto> GetMyHoroscope(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetMyHoroscopeQuery(), cancellationToken);
    }
}
