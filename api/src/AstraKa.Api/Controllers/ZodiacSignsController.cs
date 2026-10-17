using AstraKa.Application.Horoscopes;
using AstraKa.Application.ZodiacSigns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>Thin by design - this only forwards to the MediatR handler, per CLAUDE.md.</summary>
[ApiController]
[Route("api/v1/zodiac-signs")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ZodiacSignsController(ISender sender) : ControllerBase
{
    /// <summary>The 12 zodiac signs - public, no authentication required.</summary>
    /// <response code="200">Every zodiac sign.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ZodiacSignDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ZodiacSignDto>> Get(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetZodiacSignsQuery(), cancellationToken);
    }

    /// <summary>Today's horoscope for a zodiac sign - public, no authentication required. Cached per (sign, UTC date) until the end of the day.</summary>
    /// <response code="200">Today's horoscope.</response>
    /// <response code="404">No zodiac sign with this id exists.</response>
    [HttpGet("{signId:int}/horoscope")]
    [ProducesResponseType<HoroscopeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<HoroscopeDto> GetHoroscope(int signId, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetHoroscopeQuery(signId), cancellationToken);
    }
}
