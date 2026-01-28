namespace BookStore.Models;

public class Store : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Location { get; set; }
    public ICollection<StoreBook> StoreBooks { get; set; } = new List<StoreBook>();
}
