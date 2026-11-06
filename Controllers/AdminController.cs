
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Intalnirea1.Data;
using Intalnirea1.Models;

namespace Intalnirea1.Controllers
{
    public class AdminController : Controller
    {
        private readonly LibraryContext _db;

        public AdminController(LibraryContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Ratings = await _db.Rating.CountAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportRatings(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Msg"] = "Alegeți un fișier CSV.";
                return RedirectToAction(nameof(Index));
            }

            var ratings = new List<Rating>();

            using var reader = new StreamReader(file.OpenReadStream());

            await reader.ReadLineAsync();

            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                var p = line.Split(',');

                if (p.Length < 3 ||
                    !int.TryParse(p[0], out var u) ||
                    !int.TryParse(p[1], out var b) ||
                    !int.TryParse(p[2], out var s))
                    continue;

                ratings.Add(new Rating
                {
                    ReaderId = u,
                    BookID = b,
                    Stars = s,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _db.Rating.AddRange(ratings);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Au fost importate {ratings.Count} evaluări.";

            return RedirectToAction(nameof(Index));
        }
    }
}
