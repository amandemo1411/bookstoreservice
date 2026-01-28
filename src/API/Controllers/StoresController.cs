using BookStore.BusinessLogic.Interfaces;
using BookStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;
    private readonly ILogger<StoresController> _logger;

    public StoresController(IStoreService storeService, ILogger<StoresController> logger)
    {
        _storeService = storeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StoreDto>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/stores");

        var result = await _storeService.GetAllAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StoreDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/stores/{Id}", id);

        var result = await _storeService.GetByIdAsync(id, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("GetById for store {Id} failed: {Error}", id, result.Error);
            return BadRequest(result.Error);
        }

        if (result.Value is null)
        {
            _logger.LogWarning("Store {Id} not found", id);
            return NotFound();
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<StoreDto>> Create([FromBody] CreateStoreRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /api/stores");

        var result = await _storeService.CreateAsync(request, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("Create store failed: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}/books")]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooksForStore(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/stores/{Id}/books", id);

        var result = await _storeService.GetBooksForStoreAsync(id, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "Store not found.")
            {
                _logger.LogWarning("GetBooksForStore store {Id} not found", id);
                return NotFound();
            }

            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost("assign-book")]
    public async Task<ActionResult<StoreDetailsDto>> AssignBook([FromBody] AssignBookToStoreRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /api/stores/assign-book for store {StoreId} book {BookId}",
            request.StoreId, request.BookId);

        var result = await _storeService.AssignBookToStoreAsync(request, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("AssignBook failed: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{storeId:guid}/books/{bookId:guid}")]
    public async Task<ActionResult<StoreDetailsDto>> RemoveBook(Guid storeId, Guid bookId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP DELETE /api/stores/{StoreId}/books/{BookId}", storeId, bookId);

        var result = await _storeService.RemoveBookFromStoreAsync(storeId, bookId, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "Store not found.")
            {
                _logger.LogWarning("RemoveBook store {StoreId} not found", storeId);
                return NotFound();
            }

            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }
}

