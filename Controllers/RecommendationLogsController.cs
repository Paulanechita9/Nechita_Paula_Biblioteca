
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Intalnirea1.Data;

namespace Intalnirea1.Controllers;

public class RecommendationLogsController : Controller
{
    private readonly LibraryContext _db;

    public RecommendationLogsController(LibraryContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? readerId, int page = 1)
    {
        const int pageSize = 10;
        page = Math.Max(1, page);

        var query = _db.RecommendationLog
            .AsNoTracking()
            .Include(r => r.Book)
            .AsQueryable();

        if (readerId.HasValue)
            query = query.Where(r => r.ReaderId == readerId.Value);

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.ID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.ReaderId = readerId;
        ViewBag.Page = page;
        ViewBag.TotalPages =
            (int)Math.Ceiling(total / (double)pageSize);

        return View(logs);
    }
}
