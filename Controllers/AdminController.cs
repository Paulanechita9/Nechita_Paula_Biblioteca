
using Intalnirea1.Data;
using Intalnirea1.Models;
using Intalnirea1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Intalnirea1.Controllers
{
    public class AdminController : Controller
    {
        private readonly LibraryContext _db;
        private readonly Intalnirea1.Services.RecommenderTrainer _trainer;
       
        private readonly Intalnirea1.Services.GenreClassifier _genreClassifier;

        public AdminController(
     LibraryContext db,
     Intalnirea1.Services.RecommenderTrainer trainer,
     Intalnirea1.Services.GenreClassifier genreClassifier)
        {
            _db = db;
            _trainer = trainer;
            _genreClassifier = genreClassifier;
        }



        public async Task<IActionResult> Index()
        {
            ViewBag.Ratings = await _db.Rating.CountAsync();

            ViewBag.TotalBooks = await _db.Book.CountAsync();

            ViewBag.TotalReaders = await _db.Rating
                .Select(r => r.ReaderId)
                .Distinct()
                .CountAsync();

            ViewBag.AverageRating = await _db.Rating
                .AverageAsync(r => (double?)r.Stars) ?? 0;

            ViewBag.TotalRecommendations =
                await _db.RecommendationLog.CountAsync();

            var authorStats = await _db.Rating
                .Join(_db.Book,
                    rating => rating.BookID,
                    book => book.ID,
                    (rating, book) => new { rating, book })
                .GroupBy(x => x.book.AuthorID)
                .Select(g => new
                {
                    AuthorId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            ViewBag.AuthorStats = authorStats;
            ViewBag.AuthorLabels = authorStats
    .Select(x => $"Autor #{x.AuthorId}")
    .ToArray();

            ViewBag.AuthorCounts = authorStats
                .Select(x => x.Count)
                .ToArray();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retrain()

        {
            var result = await _trainer.TrainAsync();

            TempData["Msg"] =
                $"Model reantrenat cu {result.Count} evaluări. RMSE: {result.Rmse:F4}";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TrainGenre()
        {
            var result = _genreClassifier.Train();

            TempData["Msg"] =
                $"Clasificator antrenat! MacroAccuracy: {result.Accuracy:F4}, MicroAccuracy: {result.MicroAccuracy:F4}";

            return RedirectToAction(nameof(Index));
        }

    }
}
