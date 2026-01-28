using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BookStore.BusinessLogic.Interfaces;

namespace BookStore.BusinessLogic.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _authorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthorService> _logger;
    private readonly IMemoryCache _cache;

    private const string AuthorsAllCacheKey = "authors_all";

    public AuthorService(
        IAuthorRepository authorRepository,
        IUnitOfWork unitOfWork,
        ILogger<AuthorService> logger,
        IMemoryCache cache)
    {
        _authorRepository = authorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<AuthorDto>> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating author {@Request}", request);

        var exists = await _authorRepository.ExistsByNameAsync(request.FirstName, request.LastName, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Attempted to create duplicate author {FirstName} {LastName}", request.FirstName, request.LastName);
            return Result<AuthorDto>.Fail("Author with the same first and last name already exists.");
        }

        var author = new Author
        {
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _authorRepository.AddAsync(author);
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        // Invalidate cached authors list
        _cache.Remove(AuthorsAllCacheKey);

        _logger.LogInformation("Author created with Id {AuthorId}", author.Id);

        return Result<AuthorDto>.Ok(new AuthorDto(author.Id, author.FirstName, author.LastName));
    }

    public async Task<Result<IReadOnlyCollection<AuthorDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all authors");

        if (!_cache.TryGetValue(AuthorsAllCacheKey, out IReadOnlyCollection<AuthorDto>? cached))
        {
            var authors = await _authorRepository.GetAllAsync(cancellationToken);

            cached = authors
                .Select(a => new AuthorDto(a.Id, a.FirstName, a.LastName))
                .ToList()
                .AsReadOnly();

            _cache.Set(AuthorsAllCacheKey, cached, TimeSpan.FromMinutes(5));
        }

        return Result<IReadOnlyCollection<AuthorDto>>.Ok(cached);
    }

    public async Task<Result<AuthorDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving author {AuthorId}", id);

        var author = await _authorRepository.GetByIdAsync(id);
        if (author is null)
        {
            _logger.LogWarning("Author {AuthorId} not found", id);
            return Result<AuthorDto?>.Ok(null);
        }

        return Result<AuthorDto?>.Ok(new AuthorDto(author.Id, author.FirstName, author.LastName));
    }
}

