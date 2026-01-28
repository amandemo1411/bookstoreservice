using BookStore.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStore.DataAccess;

public class BookStoreDbContext : DbContext
{
    public BookStoreDbContext(DbContextOptions<BookStoreDbContext> options) : base(options)
    {
    }

    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<StoreBook> StoreBooks => Set<StoreBook>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        // audit log for write/update/delete
        var auditEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .Select(e => CreateAuditLog(e))
            .Where(a => a is not null)
            .Select(a => a!);

        AuditLogs.AddRange(auditEntries);

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasIndex(b => b.Isbn)
            .IsUnique();

        modelBuilder.Entity<Author>()
            .HasIndex(a => new { a.FirstName, a.LastName })
            .IsUnique();

        modelBuilder.Entity<BookAuthor>()
            .HasKey(x => new { x.BookId, x.AuthorId });

        modelBuilder.Entity<BookAuthor>()
            .HasOne(x => x.Book)
            .WithMany(b => b.BookAuthors)
            .HasForeignKey(x => x.BookId);

        modelBuilder.Entity<BookAuthor>()
            .HasOne(x => x.Author)
            .WithMany(a => a.BookAuthors)
            .HasForeignKey(x => x.AuthorId);

        modelBuilder.Entity<StoreBook>()
            .HasKey(x => new { x.StoreId, x.BookId });

        modelBuilder.Entity<StoreBook>()
            .HasOne(x => x.Store)
            .WithMany(s => s.StoreBooks)
            .HasForeignKey(x => x.StoreId);

        modelBuilder.Entity<StoreBook>()
            .HasOne(x => x.Book)
            .WithMany(b => b.StoreBooks)
            .HasForeignKey(x => x.BookId);
    }

    private AuditLog? CreateAuditLog(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var entityName = entry.Entity.GetType().Name;
        Guid entityId = entry.Entity is BaseEntity be ? be.Id : Guid.Empty;
        var action = entry.State.ToString();

        string? changes = null;
        if (entry.State == EntityState.Modified)
        {
            var modifiedProperties = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => $"{p.Metadata.Name}:{p.OriginalValue} -> {p.CurrentValue}");
            changes = string.Join(";", modifiedProperties);
        }
        else if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
        {
            changes = $"State:{entry.State}";
        }

        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Changes = changes,
            TimestampUtc = DateTime.UtcNow
        };
    }
}
