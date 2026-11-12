// Mcp/LibraryTools.cs
using Intalnirea1.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

public record BookInfo(int Id, string Title, string? Author, string? Genre, int Year, decimal Price);

public record BookDetails(int Id, string Title, string? Author, string? Genre, int Year,
                          decimal Price, string? Description, int Orders);

[McpServerToolType]
public class LibraryTools
{
    [McpServerTool(Name = "search_books", ReadOnly = true, Destructive = false, OpenWorld = false)]


    [Description("Cauta carti in catalogul Bibliotecii dupa un cuvant din titlu sau din numele autorului. " +
             "Intoarce cel mult 10 carti, cu ID, titlu, autor, gen, an si pret.")]

    public static async Task<List<BookInfo>> SearchBooks(
        LibraryContext db,                                    
        [Description("Cuvantul cautat, de exemplu 'Rebreanu' sau 'Ion'.")] string query,
        CancellationToken ct)
    {
        var q = query.Trim();
        return await db.Book.AsNoTracking()
            .Where(b => b.Title.Contains(q)
                     || (b.Author != null && (b.Author.LastName.Contains(q) || b.Author.FirstName.Contains(q))))
            .OrderBy(b => b.Title)
            .Take(10)
            .Select(b => new BookInfo(b.ID, b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name,
                b.Year, b.Price))
            .ToListAsync(ct);
    }

    [McpServerTool(Name = "get_book_details", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Intoarce detaliile unei carti dupa ID: autor, gen, an, pret, descriere si numarul de comenzi. " +
                 "Daca nu stii ID-ul, cauta intai cartea cu search_books.")]
    public static async Task<BookDetails?> GetBookDetails(
        LibraryContext db,
        [Description("ID-ul cartii din catalog.")] int bookId,
        CancellationToken ct)
    {
        return await db.Book.AsNoTracking()
            .Where(b => b.ID == bookId)
            .Select(b => new BookDetails(b.ID, b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name,
                b.Year, b.Price, b.Description, b.Orders.Count()))
            .FirstOrDefaultAsync(ct);
    }

    [McpServerTool(Name = "recommend_books", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Recomandă carti unui cititor, dupa evaluarile lui (modelul ML.NET al Bibliotecii). " +
                 "Pentru un cititor fara evaluari intoarce cele mai bine evaluate carti (Fallback = true).")]
    public static async Task<List<Recommendation>> RecommendBooks(
        RecommendationService recs,                           // serviciul din I6
        [Description("ID-ul cititorului (ReaderId).")] int readerId,
        [Description("Câte recomandări (1–10). Implicit 5.")] int top = 5,
        CancellationToken ct = default)
    {
        return await recs.RecommendAsync(readerId, Math.Clamp(top, 1, 10), ct);
    }

    // ---------- Misiunea 3 · tool-ul nou  ----------
     [McpServerTool(Name = "top_books_by_price", ReadOnly = true, Destructive = false, OpenWorld = false)]
     [Description("Intoarce cele mai scumpe sau cele mai ieftine carti din catalog.")]
     public static async Task<List<BookInfo>> TopBooksByPrice(
        LibraryContext db,
         [Description("true = cele mai scumpe; false = cele mai ieftine.")] bool mostExpensive,
         [Description("Câte cărți (1–10). Implicit 3.")] int top = 3,
         CancellationToken ct = default)
     {
         var books = db.Book.AsNoTracking();
         books = mostExpensive ? books.OrderByDescending(b => b.Price) : books.OrderBy(b => b.Price);
        return await books.Take(Math.Clamp(top, 1, 10))
            .Select(b => new BookInfo(b.ID, b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name, b.Year, b.Price))
            .ToListAsync(ct);
     }

    [McpServerTool(Name = "books_by_genre", ReadOnly = true, Destructive = false, OpenWorld = false)]
    [Description("Cauta carti din biblioteca dupa genul literar. Foloseste acest instrument cand utilizatorul cere carti dintr-un anumit gen.")]
    public static async Task<List<BookInfo>> BooksByGenre(
        LibraryContext db,
        [Description("Numele genului literar cautat, de exemplu Roman sau Fantasy.")] string genre,
        CancellationToken ct = default)
    {
        return await db.Book.AsNoTracking()
            .Where(b => b.Genre != null && b.Genre.Name.Contains(genre))
            .Select(b => new BookInfo(
                b.ID,
                b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name,
                b.Year,
                b.Price))
            .ToListAsync(ct);
    }

    [McpServerTool(
        Name = "books_by_year",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false)]
    [Description("Cauta cartile publicate intr-un interval de ani. Foloseste acest instrument cand utilizatorul cere carti aparute intre doi ani.")]
    public static async Task<List<BookInfo>> BooksByYear(
        LibraryContext db,
        [Description("Anul de inceput al intervalului, inclusiv.")] int fromYear,
        [Description("Anul de sfarsit al intervalului, inclusiv.")] int toYear,
        CancellationToken ct = default)
    {
        if (fromYear <= 0 || toYear <= 0)
            throw new McpException(
                "Anii trebuie sa fie numere pozitive. Introdu doi ani valizi.");

        if (fromYear > toYear)
            throw new McpException(
                "Interval invalid: anul de inceput trebuie sa fie mai mic sau egal cu anul de sfarsit. Inverseaza anii.");

        var books = await db.Book
            .AsNoTracking()
            .Where(b => b.Year >= fromYear && b.Year <= toYear)
            .OrderBy(b => b.Year)
            .ThenBy(b => b.Title)
            .Take(10)
            .Select(b => new BookInfo(
                b.ID,
                b.Title,
                b.Author == null ? null : b.Author.FirstName + " " + b.Author.LastName,
                b.Genre == null ? null : b.Genre.Name,
                b.Year,
                b.Price))
            .ToListAsync(ct);

        if (books.Count == 0)
            throw new McpException(
                $"Nu exista carti intre anii {fromYear} si {toYear}. Incearca un interval mai larg.");

        return books;
    }

}
