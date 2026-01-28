using BookStore.BusinessLogic.Interfaces;
using BookStore.DataAccess;
using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BookStore.BusinessLogic.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookService> _logger;
    private readonly IMemoryCache _cache;

    private static string BookDetailsCacheKey(Guid id) => $"book_details_{id}";

    public BookService(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        IStoreRepository storeRepository,
        IUnitOfWork unitOfWork,
        ILogger<BookService> logger,
        IMemoryCache cache)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<BookDetailsDto>> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating book {@Request}", request);

        var existing = await _bookRepository.GetByIsbnAsync(request.Isbn, cancellationToken);

        if (existing is not null)
        {
            _logger.LogWarning("Attempted to create duplicate book with ISBN {Isbn}", request.Isbn);
            return Result<BookDetailsDto>.Fail("Book with the same ISBN already exists.");
        }

        var book = new Book
        {
            Isbn = request.Isbn,
            Title = request.Title,
            Description = request.Description
        };

        await AttachAuthorsAsync(book, request.AuthorIds, cancellationToken);
        await AttachStoresAsync(book, request.StoreIds, cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _bookRepository.AddAsync(book);
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        _logger.LogInformation("Book created with Id {BookId}", book.Id);

        // Invalidate cache for this book
        _cache.Remove(BookDetailsCacheKey(book.Id));

        var details = await GetDetailsByIdInternalAsync(book.Id, cancellationToken);
        return details.Success
            ? Result<BookDetailsDto>.Ok(details.Value!)
            : Result<BookDetailsDto>.Fail(details.Error ?? "Failed to load created book.");
    }

    public async Task<Result<BookDetailsDto>> UpdateAsync(Guid id, UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating book {BookId}", id);

        var book = await _bookRepository.GetDetailsAsync(id, cancellationToken);

        if (book is null)
        {
            _logger.LogWarning("Book {BookId} not found for update", id);
            return Result<BookDetailsDto>.Fail("Book not found.");
        }

        book.Title = request.Title;
        book.Description = request.Description;

        if (request.AuthorIds is not null)
        {
            book.BookAuthors.Clear();
            await AttachAuthorsAsync(book, request.AuthorIds, cancellationToken);
        }

        if (request.StoreIds is not null)
        {
            book.StoreBooks.Clear();
            await AttachStoresAsync(book, request.StoreIds, cancellationToken);
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        // Invalidate cache for this book
        _cache.Remove(BookDetailsCacheKey(book.Id));

        var details = await GetDetailsByIdInternalAsync(book.Id, cancellationToken);
        return details.Success
            ? Result<BookDetailsDto>.Ok(details.Value!)
            : Result<BookDetailsDto>.Fail(details.Error ?? "Failed to load updated book.");
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting book {BookId}", id);

        var book = await _bookRepository.GetByIdAsync(id);
        if (book is null)
        {
            _logger.LogWarning("Book {BookId} not found for delete", id);
            return Result<bool>.Fail("Book not found.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _bookRepository.DeleteAsync(book);
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        // Invalidate cache for this book
        _cache.Remove(BookDetailsCacheKey(book.Id));

        return Result<bool>.Ok(true);
    }

    public async Task<Result<BookDetailsDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving book details for {BookId}", id);

        var details = await GetDetailsByIdInternalAsync(id, cancellationToken);
        if (!details.Success)
        {
            return Result<BookDetailsDto?>.Fail(details.Error ?? "Failed to load book.");
        }

        return Result<BookDetailsDto?>.Ok(details.Value);
    }

    public async Task<Result<PagedResult<BookDto>>> GetBooksAsync(BookFilterRequest filter, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving books with filter {@Filter}", filter);

        var pagedBooks = await _bookRepository.GetPagedAsync(filter, cancellationToken);

        var dtoItems = pagedBooks.Items
            .Select(b => new BookDto(b.Id, b.Isbn, b.Title, b.Description))
            .ToList();

        var dtoPaged = new PagedResult<BookDto>
        {
            Items = dtoItems,
            Page = pagedBooks.Page,
            PageSize = pagedBooks.PageSize,
            Total = pagedBooks.Total
        };

        return Result<PagedResult<BookDto>>.Ok(dtoPaged);
    }

    public async Task<Result<BookDetailsDto>> AssignAuthorToBookAsync(AssignAuthorToBookRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assigning author {AuthorId} to book {BookId}", request.AuthorId, request.BookId);

        var book = await _bookRepository.GetWithAuthorsAsync(request.BookId, cancellationToken);
        if (book is null)
        {
            _logger.LogWarning("Book {BookId} not found when assigning author", request.BookId);
            return Result<BookDetailsDto>.Fail("Book not found.");
        }

        var author = await _authorRepository.GetByIdAsync(request.AuthorId);
        if (author is null)
        {
            _logger.LogWarning("Author {AuthorId} not found when assigning to book {BookId}", request.AuthorId, request.BookId);
            return Result<BookDetailsDto>.Fail("Author not found.");
        }

        if (!book.BookAuthors.Any(ba => ba.AuthorId == author.Id))
        {
            book.BookAuthors.Add(new BookAuthor
            {
                BookId = book.Id,
                AuthorId = author.Id
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache for this book
        _cache.Remove(BookDetailsCacheKey(book.Id));

        var details = await GetDetailsByIdInternalAsync(book.Id, cancellationToken);
        return details.Success
            ? Result<BookDetailsDto>.Ok(details.Value!)
            : Result<BookDetailsDto>.Fail(details.Error ?? "Failed to load updated book.");
    }

    public async Task<Result<BookDetailsDto>> RemoveAuthorFromBookAsync(Guid bookId, Guid authorId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing author {AuthorId} from book {BookId}", authorId, bookId);

        var book = await _bookRepository.GetWithAuthorsAsync(bookId, cancellationToken);
        if (book is null)
        {
            _logger.LogWarning("Book {BookId} not found when removing author", bookId);
            return Result<BookDetailsDto>.Fail("Book not found.");
        }

        var link = book.BookAuthors.FirstOrDefault(ba => ba.AuthorId == authorId);
        if (link is null)
        {
            _logger.LogWarning("Author {AuthorId} is not assigned to book {BookId}", authorId, bookId);
            return Result<BookDetailsDto>.Fail("Author is not assigned to this book.");
        }

        book.BookAuthors.Remove(link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache for this book
        _cache.Remove(BookDetailsCacheKey(book.Id));

        var details = await GetDetailsByIdInternalAsync(book.Id, cancellationToken);
        return details.Success
            ? Result<BookDetailsDto>.Ok(details.Value!)
            : Result<BookDetailsDto>.Fail(details.Error ?? "Failed to load updated book.");
    }

    private async Task<Result<BookDetailsDto>> GetDetailsByIdInternalAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_cache.TryGetValue(BookDetailsCacheKey(id), out BookDetailsDto? cached))
        {
            var book = await _bookRepository.GetDetailsAsync(id, cancellationToken);

            if (book is null)
            {
                return Result<BookDetailsDto>.Fail("Book not found.");
            }

            var authors = book.BookAuthors
            .Select(ba => ba.Author)
            .Distinct()
            .Select(a => new AuthorDto(a.Id, a.FirstName, a.LastName))
            .ToList()
            .AsReadOnly();

            var stores = book.StoreBooks
            .Select(sb => sb.Store)
            .Distinct()
            .Select(s => new StoreDto(s.Id, s.Name, s.Location))
            .ToList()
            .AsReadOnly();

            cached = new BookDetailsDto(book.Id, book.Isbn, book.Title, book.Description, authors, stores);
            _cache.Set(BookDetailsCacheKey(id), cached, TimeSpan.FromMinutes(5));
        }

        return Result<BookDetailsDto>.Ok(cached);
    }

    private async Task AttachAuthorsAsync(Book book, IEnumerable<Guid>? authorIds, CancellationToken cancellationToken)
    {
        if (authorIds is null)
        {
            return;
        }

        var ids = authorIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var authors = await _authorRepository.GetByIdsAsync(ids, cancellationToken);

        foreach (var author in authors)
        {
            book.BookAuthors.Add(new BookAuthor
            {
                BookId = book.Id,
                AuthorId = author.Id
            });
        }
    }

    private async Task AttachStoresAsync(Book book, IEnumerable<Guid>? storeIds, CancellationToken cancellationToken)
    {
        if (storeIds is null)
        {
            return;
        }

        var ids = storeIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var stores = await _storeRepository.GetByIdsAsync(ids, cancellationToken);

        foreach (var store in stores)
        {
            book.StoreBooks.Add(new StoreBook
            {
                StoreId = store.Id,
                BookId = book.Id,
                Quantity = 0
            });
        }
    }
}

