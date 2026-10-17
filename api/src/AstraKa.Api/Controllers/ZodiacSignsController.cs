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
}
