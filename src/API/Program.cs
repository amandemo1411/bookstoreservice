using BookStore.BusinessLogic;
using BookStore.DataAccess;
using Microsoft.EntityFrameworkCore;
using BookStore.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// Database configuration - SQLite: use absolute path so the DB file is always in a known place
var configuredCs = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bookstore.db";
var dbPath = configuredCs.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
    ? configuredCs["Data Source=".Length..].Trim()
    : "bookstore.db";
if (!Path.IsPathRooted(dbPath))
    dbPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath ?? AppContext.BaseDirectory, dbPath));
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<BookStoreDbContext>(options =>
    options.UseSqlite(connectionString));

// Data access
builder.Services.AddDataAccess();

// Business logic services
builder.Services.AddBookStoreServices();

var app = builder.Build();

// Apply migrations on startup so InitialCreate (and __EFMigrationsHistory) exist in the database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await db.Database.MigrateAsync();
    logger.LogInformation(
        "SQLite database path: {DbPath}. Migrations applied. Open this file in DB Browser to see __EFMigrationsHistory and tables.",
        dbPath);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
