namespace BookStore.Models;

public class BookAuthor
{
    public Guid BookId { get; set; }
    public Book Book { get; set; } = default!;
    public Guid AuthorId { get; set; }
    public Author Author { get; set; } = default!;
}
