using BookStore.BusinessLogic.Interfaces;
using BookStore.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/database")]
public class SeedController : ControllerBase
{
    private readonly ISeedService _seedService;

    public SeedController(ISeedService seedService)
    {
        _seedService = seedService;
    }

    /// <summary>
    /// Seed database from JSON file via service/repository layers.
    /// </summary>
    [HttpGet("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        var result = await _seedService.SeedAsync(cancellationToken);
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, result.Error);
        }

        return Ok(result.Value);
    }
}

