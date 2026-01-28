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

// Database configuration - SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=bookstore.db";

builder.Services.AddDbContext<BookStoreDbContext>(options =>
    options.UseSqlite(connectionString));

// Data access
builder.Services.AddDataAccess();

// Business logic services
builder.Services.AddBookStoreServices();

var app = builder.Build();

// Ensure database is created on startup (for demo/dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    db.Database.EnsureCreated();
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
