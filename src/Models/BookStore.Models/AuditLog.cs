namespace BookStore.Models;

public class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = default!;
    public string? Changes { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
