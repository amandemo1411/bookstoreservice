namespace BookStore.Models;

public class StoreBook
{
    public Guid StoreId { get; set; }
    public Store Store { get; set; } = default!;
    public Guid BookId { get; set; }
    public Book Book { get; set; } = default!;
    public int Quantity { get; set; } = 0;
}
