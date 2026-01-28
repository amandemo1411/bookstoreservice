using BookStore.BusinessLogic.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookService bookService, ILogger<BooksController> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookDto>>> GetBooks([FromQuery] BookFilterRequest filter, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/books with filter {@Filter}", filter);

        var result = await _bookService.GetBooksAsync(filter, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("GetBooks failed: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET /api/books/{Id}", id);

        var result = await _bookService.GetByIdAsync(id, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("GetById for book {Id} failed: {Error}", id, result.Error);
            return BadRequest(result.Error);
        }

        if (result.Value is null)
        {
            return NotFound();
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<BookDetailsDto>> Create([FromBody] CreateBookRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /api/books");

        var result = await _bookService.CreateAsync(request, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "Book with the same ISBN already exists.")
            {
                _logger.LogWarning("Create book conflict: {Error}", result.Error);
                return Conflict(result.Error);
            }

            _logger.LogWarning("Create book failed: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BookDetailsDto>> Update(Guid id, [FromBody] UpdateBookRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP PUT /api/books/{Id}", id);

        var result = await _bookService.UpdateAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "Book not found.")
            {
                _logger.LogWarning("Update book {Id} not found", id);
                return NotFound();
            }

            _logger.LogWarning("Update book {Id} failed: {Error}", id, result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP DELETE /api/books/{Id}", id);

        var result = await _bookService.DeleteAsync(id, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "Book not found.")
            {
                _logger.LogWarning("Delete book {Id} not found", id);
                return NotFound();
            }

            _logger.LogWarning("Delete book {Id} failed: {Error}", id, result.Error);
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpPost("assign-author")]
    public async Task<ActionResult<BookDetailsDto>> AssignAuthor([FromBody] AssignAuthorToBookRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP POST /api/books/assign-author for book {BookId} and author {AuthorId}", request.BookId, request.AuthorId);

        var result = await _bookService.AssignAuthorToBookAsync(request, cancellationToken);
        if (!result.Success)
        {
            _logger.LogWarning("AssignAuthor failed: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{bookId:guid}/authors/{authorId:guid}")]
    public async Task<ActionResult<BookDetailsDto>> RemoveAuthor(Guid bookId, Guid authorId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP DELETE /api/books/{BookId}/authors/{AuthorId}", bookId, authorId);

        var result = await _bookService.RemoveAuthorFromBookAsync(bookId, authorId, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "Book not found.")
            {
                _logger.LogWarning("RemoveAuthor book {BookId} not found", bookId);
                return NotFound();
            }

            _logger.LogWarning("RemoveAuthor failed: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }
}

