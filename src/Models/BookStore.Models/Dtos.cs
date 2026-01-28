namespace BookStore.Models;

public record AuthorDto(Guid Id, string FirstName, string LastName);
public record StoreDto(Guid Id, string Name, string? Location);
public record BookDto(Guid Id, string Isbn, string Title, string? Description);

public record BookDetailsDto(
    Guid Id,
    string Isbn,
    string Title,
    string? Description,
    IReadOnlyCollection<AuthorDto> Authors,
    IReadOnlyCollection<StoreDto> Stores);

public record StoreDetailsDto(
    Guid Id,
    string Name,
    string? Location,
    IReadOnlyCollection<BookDto> Books);
