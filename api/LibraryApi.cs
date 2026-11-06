using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Intalnirea1.Data;
using Intalnirea1.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.ML;

namespace Intalnirea1.Api;


public record BookDto(int Id, string Title, string? Author, string? Genre, decimal Price);
public record NewBookDto(
    [Required, StringLength(200)] string Title,
    [Range(1.0, 10000.0)] decimal Price,
    int? AuthorID,
    int? GenreID);
public record PriceDto([Range(1.0, 10000.0)] decimal Price);
public record RecommendationDto(int BookId, string Title, double Score);
public record GenreDescriptionRequest(string Description);
public static class LibraryApi
{

    public static void MapLibraryApi(this WebApplication app)
    {
        app.MapPost("/api/v2/genre/predict",
            (GenreDescriptionRequest request,
             Intalnirea1.Services.GenreClassifier classifier) =>
            {
                if (string.IsNullOrWhiteSpace(request.Description))
                    return Results.BadRequest("Introdu o descriere.");

                var genre = classifier.Predict(request.Description);

                return Results.Ok(new { genre });
            });


        app.MapGet("/api/v2/readers/{readerId:int}/recommendations",
            async (int readerId, LibraryContext db) =>
            {
                var userRatings = await db.Rating
            .CountAsync(r => r.ReaderId == readerId);

                if (userRatings < 10)
                {
                    var popularBooks = await db.Rating
                .GroupBy(r => r.BookID)
                .Select(g => new
                    {
                        BookId = g.Key,
                        Average = g.Average(r => r.Stars),
                        Count = g.Count()
                    })
                .OrderByDescending(x => x.Average)
                .ThenByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

                    var results = new List<RecommendationDto>();

                    foreach (var item in popularBooks)
                    {
                        var book = await db.Book.FindAsync(item.BookId);

                        if (book != null)
                            results.Add(new RecommendationDto(
                        book.ID, book.Title, item.Average));
                    }

                    return Results.Ok(results);
                }


                var modelPath = Path.Combine(
                    app.Environment.ContentRootPath,
                    "MLModels",
                    "BookRecommender.zip");

                if (!File.Exists(modelPath))
                    return Results.Problem("Modelul nu a fost antrenat.");

                var ml = new MLContext();
                var model = ml.Model.Load(modelPath, out _);

                var ratedBookIds = await db.Rating
                    .Where(r => r.ReaderId == readerId)
                    .Select(r => r.BookID)
                    .ToListAsync();

                var availableBooks = await db.Book
                    .AsNoTracking()
                    .Where(b => !ratedBookIds.Contains(b.ID))
                    .Select(b => new { b.ID, b.Title })
                    .ToListAsync();

                var predictions = new List<RecommendationDto>();

                using (var engine = ml.Model.CreatePredictionEngine<
                    BookRating, BookRatingPrediction>(model))
                {
                    foreach (var book in availableBooks)
                    {
                        var prediction = engine.Predict(new BookRating
                        {
                            UserId = readerId,
                            BookId = book.ID
                        });

                        if (float.IsFinite(prediction.Score))
                        {
                            predictions.Add(new RecommendationDto(
                                book.ID,
                                book.Title,
                                prediction.Score));
                        }
                    }
                }

                return Results.Ok(
                    predictions
                        .OrderByDescending(r => r.Score)
                        .Take(5)
                        .ToList());

            });

        var books = app.MapGroup("/api/v2/books").WithTags("Books · Minimal API");

        // Lista cartilor
        books.MapGet("/", async (string? genre, int? page, LibraryContext db) =>
        {
            const int pageSize = 5;
            var p = Math.Max(page ?? 1, 1);
            var list = await db.Book
                .Where(b => genre == null || b.Genre!.Name == genre)
                .OrderBy(b => b.ID)
                .Skip((p - 1) * pageSize).Take(pageSize)
                .Select(b => new BookDto(b.ID, b.Title,
                    b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : null,
                    b.Genre != null ? b.Genre.Name : null, b.Price))
                .ToListAsync();
            return Results.Ok(list);
        });

        // Detaliile unei cărți
       
        books.MapGet("/{id:int}", async Task<Results<Ok<BookDto>, NotFound>> (int id, LibraryContext db) =>
        {
            var dto = await db.Book.Where(b => b.ID == id)
                .Select(b => new BookDto(b.ID, b.Title,
                    b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : null,
                    b.Genre != null ? b.Genre.Name : null, b.Price))
                .FirstOrDefaultAsync();
            return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
        });

        // Adaugă o carte 
        books.MapPost("/", async (NewBookDto input, LibraryContext db) =>
        {
            var book = new Book { Title = input.Title, Price = input.Price,
                                  AuthorID = input.AuthorID, GenreID = input.GenreID };
            db.Book.Add(book);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v2/books/{book.ID}",
                new BookDto(book.ID, book.Title, null, null, book.Price));
        });

        // Modifică prețul
        books.MapPatch("/{id:int}", async (int id, PriceDto input, LibraryContext db) =>
        {
            var book = await db.Book.FindAsync(id);
            if (book is null) return Results.NotFound();
            book.Price = input.Price;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Șterge o carte
        books.MapDelete("/{id:int}", async (int id, LibraryContext db) =>
        {
            var book = await db.Book.FindAsync(id);
            if (book is null) return Results.NotFound();
            db.Book.Remove(book);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
