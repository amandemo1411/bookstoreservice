namespace BookStore.Models;

public class Book : BaseEntity
{
    public string Isbn { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    public ICollection<StoreBook> StoreBooks { get; set; } = new List<StoreBook>();
}
