# BookStoreService

BookStoreService is a .NET 8 Web API that exposes a simple Book Store API backed by **SQLite** and **EF Core**.  
It is structured in clean layers (API → BusinessLogic → DataAccess) and can be run locally or via Docker / docker‑compose.

---

## 1. Solution structure

- `src/API/BookStore.API`  
  - ASP.NET Core Web API project (controllers, middleware, Program.cs).
- `src/BusinessLogic/BookStore.BusinessLogic`  
  - Interfaces (in `Interfaces`) and service implementations (in `Services`).
- `src/DataAccess/BookStore.DataAccess`  
  - `BookStoreDbContext` (EF Core model).
  - Interfaces (in `Interfaces`) and repository/unit of work implementations (in `Repositories`).
- `src/Models/BookStore.Models`  
  - Domain entities, DTOs, request models.
- `src/Utilities/BookStore.Utilities`  
  - Generic `Result<T>`, `PagedResult<T>`, pagination helpers.
- `devops/docker-compose.yml`  
  - Orchestrates the API container and a volume for the SQLite database file.

Tests (xUnit + Moq):

- `tests/BookStore.API.Tests`
- `tests/BookStore.BusinessLogic.Tests`
- `tests/BookStore.DataAccess.Tests`

---

## 2. Prerequisites

### Local development

- **.NET 8 SDK**  
  Download from `https://dotnet.microsoft.com/download/dotnet/8.0`.

- **SQLite**  
  No separate server needed (SQLite is file‑based). EF Core will create and manage the `*.db` file automatically.

### Docker / docker‑compose

- **Docker Desktop** (or Docker Engine + docker‑compose)  
  - `https://www.docker.com/products/docker-desktop/`

The Docker setup in this repo uses:

- `mcr.microsoft.com/dotnet/aspnet:8.0` as runtime image.
- `mcr.microsoft.com/dotnet/sdk:8.0` as build image.

---

## 3. How the database is created (SQLite + EF Core)

### Connection string

In `Program.cs`:

- The connection string is read from configuration (`DefaultConnection`).  
- If not provided, it falls back to:  
  `Data Source=bookstore.db` (in the working directory).

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=bookstore.db";

builder.Services.AddDbContext<BookStoreDbContext>(options =>
    options.UseSqlite(connectionString));
```

### Creating tables

On application startup, the API ensures that the database and all EF‑mapped tables exist:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    db.Database.EnsureCreated(); // Creates the SQLite DB + schema if missing
}
```

`BookStoreDbContext` defines:

- `DbSet<Book>`, `DbSet<Author>`, `DbSet<Store>`, `DbSet<BookAuthor>`, `DbSet<StoreBook>`, `DbSet<AuditLog>`.
- `OnModelCreating(...)` configures:
  - Unique ISBN for books.
  - Unique (FirstName + LastName) for authors.
  - Composite keys and relationships for `BookAuthor` and `StoreBook` join tables.

When `EnsureCreated()` is called against an empty SQLite file, EF Core creates all tables and constraints based on this model.

---

## 4. Seeding data from JSON (Database / Seed controller)

Seed endpoint: **`GET /api/database/seed`**

Flow:

1. `SeedController` (API) → calls `ISeedService.SeedAsync()`.
2. `SeedService` (BusinessLogic) → checks if the database is already seeded and delegates to `ISeedRepository` when seeding is required.
3. `SeedRepository` (DataAccess) → reads JSON, creates authors / stores / books, then join entities, inside a transaction.

### JSON file

`src/API/Seed/seed-data.json` (copied to the output directory) contains authors, stores, books, and relations:

```json
{
  "authors": [
    { "id": "author1", "firstName": "George", "lastName": "Orwell" },
    { "id": "author2", "firstName": "Jane", "lastName": "Austen" }
  ],
  "stores": [
    { "id": "store1", "name": "Central Bookstore", "location": "Downtown" },
    { "id": "store2", "name": "Neighborhood Books", "location": "Suburbs" }
  ],
  "books": [
    {
      "id": "book1",
      "isbn": "9780451524935",
      "title": "1984",
      "description": "Dystopian novel."
    },
    {
      "id": "book2",
      "isbn": "9780141439518",
      "title": "Pride and Prejudice",
      "description": "Classic romance."
    }
  ],
  "bookAuthors": [
    { "bookId": "book1", "authorId": "author1" },
    { "bookId": "book2", "authorId": "author2" }
  ],
  "storeBooks": [
    { "storeId": "store1", "bookId": "book1", "quantity": 5 },
    { "storeId": "store1", "bookId": "book2", "quantity": 3 },
    { "storeId": "store2", "bookId": "book2", "quantity": 2 }
  ]
}
```

You can change seed data by editing this file only; no code changes required.

---

## 5. Running the application locally (without Docker)

1. Restore packages and build:
   - In Visual Studio: Build the solution.
   - CLI (if you have `dotnet` on PATH): `dotnet build` from `BookStoreService` root.

2. Run the API:
   - In Visual Studio: Set `BookStore.API` as startup project and run (F5).
   - CLI: `dotnet run --project src/API/BookStore.API.csproj`.

3. Seed the database:
   - After the API is running, call `GET /api/database/seed` (e.g. via Swagger UI or Postman).

---

## 6. Running via Docker / docker-compose

### Dockerfile (API)

Located at `src/API/Dockerfile`, it:

1. Uses the .NET 8 SDK image to restore and publish the API.
2. Uses the ASP.NET 8 runtime image to host the published bits.

### docker-compose

Located at `devops/docker-compose.yml`:

- Builds the API image from the Dockerfile.
- Exposes the API on port `5000` (mapped to container port `8080`).
- Mounts a named volume `sqlite_data` at `/app/data`.
- Overrides the connection string so the SQLite DB file lives in `/app/data/bookstore.db`.

```yaml
version: "3.9"

services:
  bookstore-api:
    build:
      context: ..
      dockerfile: src/API/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ConnectionStrings__DefaultConnection: "Data Source=/app/data/bookstore.db"
    volumes:
      - sqlite_data:/app/data
    ports:
      - "5000:8080"

volumes:
  sqlite_data:
    driver: local
```

#### Run with Docker

From the `devops` folder:

```bash
docker compose up --build
```

Then navigate to `http://localhost:5000/swagger` to explore the API and seed the database via `GET /api/database/seed`.

---

## 7. Logging, correlation ID, global error handling

- **Global exception handling**: `GlobalExceptionHandlingMiddleware` catches unhandled exceptions, logs them, and returns a JSON problem response with a `traceId`.
- **Correlation ID middleware**: `CorrelationIdMiddleware`:
  - Looks for `X-Correlation-Id` in the request.
  - If provided and valid GUID → reused.
  - Otherwise generates a new GUID, sets it as `HttpContext.TraceIdentifier`, and returns it in `X-Correlation-Id` response header.
  - Creates a logging scope `{ CorrelationId = ... }` so all logs for a request share the same correlation ID.
- **Service / controller logs**: All key operations (create/update/delete, seed, queries) log at appropriate levels (`Information`, `Warning`, `Error`) and automatically include the correlation ID via the logging scope.

---

## 8. Tests

The solution includes xUnit + Moq tests for each layer:

- **API tests** (`tests/BookStore.API.Tests`):
  - Controller tests for `AuthorsController`, `BooksController`, `SeedController` using mocked services.
- **BusinessLogic tests** (`tests/BookStore.BusinessLogic.Tests`):
  - `AuthorServiceTests`, `SeedServiceTests` using mocked repositories and unit of work.
- **DataAccess tests** (`tests/BookStore.DataAccess.Tests`):
  - `BookRepositoryTests` using **EF Core InMemory** to validate query logic.

Run all tests from Visual Studio Test Explorer or via CLI (`dotnet test`) from the solution root.

