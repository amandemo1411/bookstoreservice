using BookStore.DataAccess.Interfaces;
using BookStore.Models;
using BookStore.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BookStore.BusinessLogic.Interfaces;

namespace BookStore.BusinessLogic.Services;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _storeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StoreService> _logger;
    private readonly IMemoryCache _cache;

    private const string StoresAllCacheKey = "stores_all";
    private static string StoreDetailsCacheKey(Guid id) => $"store_details_{id}";
    private static string StoreBooksCacheKey(Guid id) => $"store_books_{id}";

    public StoreService(
        IStoreRepository storeRepository,
        IUnitOfWork unitOfWork,
        ILogger<StoreService> logger,
        IMemoryCache cache)
    {
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<StoreDto>> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating store {@Request}", request);

        var store = new Store
        {
            Name = request.Name,
            Location = request.Location
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _storeRepository.AddAsync(store);
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        // Invalidate store-related caches
        _cache.Remove(StoresAllCacheKey);
        _cache.Remove(StoreDetailsCacheKey(store.Id));
        _cache.Remove(StoreBooksCacheKey(store.Id));

        _logger.LogInformation("Store created with Id {StoreId}", store.Id);

        return Result<StoreDto>.Ok(new StoreDto(store.Id, store.Name, store.Location));
    }

    public async Task<Result<IReadOnlyCollection<StoreDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all stores");

        if (!_cache.TryGetValue(StoresAllCacheKey, out IReadOnlyCollection<StoreDto>? cached))
        {
            var stores = await _storeRepository.GetAllAsync(cancellationToken);

            cached = stores
                .Select(s => new StoreDto(s.Id, s.Name, s.Location))
                .ToList()
                .AsReadOnly();

            _cache.Set(StoresAllCacheKey, cached, TimeSpan.FromMinutes(5));
        }

        return Result<IReadOnlyCollection<StoreDto>>.Ok(cached);
    }

    public async Task<Result<StoreDetailsDto?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving store details for {StoreId}", id);

        if (!_cache.TryGetValue(StoreDetailsCacheKey(id), out StoreDetailsDto? cached))
        {
            var store = await _storeRepository.GetWithBooksAsync(id, trackChanges: false, cancellationToken);

            if (store is null)
            {
                _logger.LogWarning("Store {StoreId} not found", id);
                return Result<StoreDetailsDto?>.Ok(null);
            }

            var books = store.StoreBooks
                .Select(sb => sb.Book)
                .Distinct()
                .Select(b => new BookDto(b.Id, b.Isbn, b.Title, b.Description))
                .ToList()
                .AsReadOnly();

            cached = new StoreDetailsDto(store.Id, store.Name, store.Location, books);
            _cache.Set(StoreDetailsCacheKey(id), cached, TimeSpan.FromMinutes(5));
        }

        return Result<StoreDetailsDto?>.Ok(cached);
    }

    public async Task<Result<IReadOnlyCollection<BookDto>>> GetBooksForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving books for store {StoreId}", storeId);

        if (!_cache.TryGetValue(StoreBooksCacheKey(storeId), out IReadOnlyCollection<BookDto>? cached))
        {
            var store = await _storeRepository.GetWithBooksAsync(storeId, trackChanges: false, cancellationToken);

            if (store is null)
            {
                _logger.LogWarning("Store {StoreId} not found", storeId);
                return Result<IReadOnlyCollection<BookDto>>.Fail("Store not found.");
            }

            cached = store.StoreBooks
                .Select(sb => sb.Book)
                .Distinct()
                .Select(b => new BookDto(b.Id, b.Isbn, b.Title, b.Description))
                .ToList()
                .AsReadOnly();

            _cache.Set(StoreBooksCacheKey(storeId), cached, TimeSpan.FromMinutes(5));
        }

        return Result<IReadOnlyCollection<BookDto>>.Ok(cached);
    }

    public async Task<Result<StoreDetailsDto>> AssignBookToStoreAsync(AssignBookToStoreRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assigning book {BookId} to store {StoreId} with quantity {Quantity}",
            request.BookId, request.StoreId, request.Quantity);

        var store = await _storeRepository.GetWithBooksAsync(request.StoreId, trackChanges: true, cancellationToken);

        if (store is null)
        {
            _logger.LogWarning("Store {StoreId} not found when assigning book", request.StoreId);
            return Result<StoreDetailsDto>.Fail("Store not found.");
        }

        var existing = store.StoreBooks.FirstOrDefault(sb => sb.BookId == request.BookId);
        if (existing is null)
        {
            store.StoreBooks.Add(new StoreBook
            {
                StoreId = store.Id,
                BookId = request.BookId,
                Quantity = request.Quantity
            });
        }
        else
        {
            existing.Quantity = request.Quantity;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate store-related caches
        _cache.Remove(StoreDetailsCacheKey(store.Id));
        _cache.Remove(StoreBooksCacheKey(store.Id));

        return await GetStoreDetailsInternalAsync(store.Id, cancellationToken);
    }

    public async Task<Result<StoreDetailsDto>> RemoveBookFromStoreAsync(Guid storeId, Guid bookId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing book {BookId} from store {StoreId}", bookId, storeId);

        var store = await _storeRepository.GetWithBooksAsync(storeId, trackChanges: true, cancellationToken);

        if (store is null)
        {
            _logger.LogWarning("Store {StoreId} not found when removing book {BookId}", storeId, bookId);
            return Result<StoreDetailsDto>.Fail("Store not found.");
        }

        var storeBook = store.StoreBooks.FirstOrDefault(sb => sb.BookId == bookId);
        if (storeBook is null)
        {
            _logger.LogWarning("Book {BookId} is not assigned to store {StoreId}", bookId, storeId);
            return Result<StoreDetailsDto>.Fail("Book is not assigned to this store.");
        }

        store.StoreBooks.Remove(storeBook);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate store-related caches
        _cache.Remove(StoreDetailsCacheKey(store.Id));
        _cache.Remove(StoreBooksCacheKey(store.Id));

        return await GetStoreDetailsInternalAsync(store.Id, cancellationToken);
    }

    private async Task<Result<StoreDetailsDto>> GetStoreDetailsInternalAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetWithBooksAsync(storeId, trackChanges: false, cancellationToken);

        if (store is null)
        {
            return Result<StoreDetailsDto>.Fail("Store not found.");
        }

        var books = store.StoreBooks
            .Select(sb => sb.Book)
            .Distinct()
            .Select(b => new BookDto(b.Id, b.Isbn, b.Title, b.Description))
            .ToList()
            .AsReadOnly();

        var dto = new StoreDetailsDto(store.Id, store.Name, store.Location, books);
        return Result<StoreDetailsDto>.Ok(dto);
    }
}

