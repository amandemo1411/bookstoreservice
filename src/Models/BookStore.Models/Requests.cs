namespace BookStore.Models;

public record CreateAuthorRequest(string FirstName, string LastName);

public record CreateStoreRequest(string Name, string? Location);

public record CreateBookRequest(string Isbn, string Title, string? Description, IEnumerable<Guid>? AuthorIds, IEnumerable<Guid>? StoreIds);

public record UpdateBookRequest(string Title, string? Description, IEnumerable<Guid>? AuthorIds, IEnumerable<Guid>? StoreIds);

public record AssignBookToStoreRequest(Guid StoreId, Guid BookId, int Quantity);

public record AssignAuthorToBookRequest(Guid BookId, Guid AuthorId);

public record BookFilterRequest(string? Title, string? AuthorName, int Page = 1, int PageSize = 20, string? SortBy = null, bool Desc = false);
