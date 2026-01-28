using BookStore.BusinessLogic.Interfaces;
using BookStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;
    private readonly ILogger<AuthorsController> _logger;

    public AuthorsController(IAuthorService authorService, ILogger<AuthorsController> logger)
    {
        _authorService = authorService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/authors");

        var result = await _authorService.GetAllAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuthorDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/authors/{Id}", id);

        var result = await _authorService.GetByIdAsync(id, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("GetById for author {Id} failed: {Error}", id, result.Error);
            return BadRequest(result.Error);
        }

        if (result.Value is null)
        {
            _logger.LogWarning("Author {Id} not found", id);
            return NotFound();
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<AuthorDto>> Create([FromBody] CreateAuthorRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /api/authors");

        var result = await _authorService.CreateAsync(request, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("Create author failed: {Error}", result.Error);
            return Conflict(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }
}

