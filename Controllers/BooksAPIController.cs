using Intalnirea1.Api;
using Intalnirea1.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Intalnirea1.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BooksApiController(LibraryContext db) : ControllerBase
    {
        // GET /api/books?genre=SF
        [HttpGet]
        public async Task<IEnumerable<BookDto>> GetAll(string? genre) =>
            await db.Book
                .Where(b => genre == null || b.Genre!.Name == genre)
                .Select(b => new BookDto(b.ID, b.Title,
                    b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : null,
                    b.Genre != null ? b.Genre.Name : null, b.Price))
                .ToListAsync();

        // GET /api/books/3
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BookDto>> Get(int id)
        {
            var dto = await db.Book.Where(b => b.ID == id)
                .Select(b => new BookDto(b.ID, b.Title,
                    b.Author != null ? b.Author.FirstName + " " + b.Author.LastName : null,
                    b.Genre != null ? b.Genre.Name : null, b.Price))
                .FirstOrDefaultAsync();
            if (dto is null) return NotFound();
            return dto;
        }
    }
}
