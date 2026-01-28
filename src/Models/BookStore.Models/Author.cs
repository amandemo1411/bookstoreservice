namespace BookStore.Models;

public class Author : BaseEntity
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
