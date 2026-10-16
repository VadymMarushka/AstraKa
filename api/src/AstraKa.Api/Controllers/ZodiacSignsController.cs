using AstraKa.Application.ZodiacSigns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AstraKa.Api.Controllers;

/// <summary>Thin by design - this only forwards to the MediatR handler, per CLAUDE.md.</summary>
[ApiController]
[Route("api/v1/zodiac-signs")]
public sealed class ZodiacSignsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ZodiacSignDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ZodiacSignDto>> Get(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetZodiacSignsQuery(), cancellationToken);
    }
}
