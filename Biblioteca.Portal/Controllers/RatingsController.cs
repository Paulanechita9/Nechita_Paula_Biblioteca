
using BibliotecaGrpc;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Portal.Controllers;

public class RatingsController(
    ReaderRatings.ReaderRatingsClient client) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int readerId = 1212)
    {
        ViewBag.ReaderId = readerId;

        var ratings = new List<RatingItem>();

        try
        {
            using var call = client.ListRatings(
                new ReaderRequest { ReaderId = readerId });

            await foreach (var item in call.ResponseStream.ReadAllAsync())
            {
                ratings.Add(item);
            }
        }
        catch (RpcException ex)
        {
            ViewBag.Error = $"{ex.StatusCode}: {ex.Status.Detail}";
        }

        return View(ratings);
    }

    [HttpGet]
    public IActionResult Create(int readerId = 1212)
    {
        ViewBag.ReaderId = readerId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int readerId, int bookId, int stars)
    {
        ViewBag.ReaderId = readerId;

        try
        {
            await client.AddRatingAsync(new NewRating
            {
                ReaderId = readerId,
                BookId = bookId,
                Stars = stars
            });

            return RedirectToAction(nameof(Index), new { readerId });
        }
        catch (RpcException ex)
        {
            ViewBag.Error = $"{ex.StatusCode}: {ex.Status.Detail}";
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int readerId = 1212)
    {
        try
        {
            var rating = await client.GetRatingAsync(new RatingId { Id = id });
            ViewBag.ReaderId = readerId;
            return View(rating);
        }
        catch (RpcException ex)
        {
            TempData["Error"] = $"{ex.StatusCode}: {ex.Status.Detail}";
            return RedirectToAction(nameof(Index), new { readerId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, int readerId, int stars)
    {
        try
        {
            await client.UpdateRatingAsync(new UpdateRatingRequest
            {
                Id = id,
                Stars = stars
            });

            return RedirectToAction(nameof(Index), new { readerId });
        }
        catch (RpcException ex)
        {
            ViewBag.ReaderId = readerId;
            ViewBag.Error = $"{ex.StatusCode}: {ex.Status.Detail}";

            var rating = new RatingItem
            {
                Id = id,
                Stars = stars
            };

            return View(rating);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, int readerId = 1212)
    {
        try
        {
            var rating = await client.GetRatingAsync(new RatingId { Id = id });
            ViewBag.ReaderId = readerId;
            return View(rating);
        }
        catch (RpcException ex)
        {
            TempData["Error"] = $"{ex.StatusCode}: {ex.Status.Detail}";
            return RedirectToAction(nameof(Index), new { readerId });
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, int readerId)
    {
        try
        {
            await client.DeleteRatingAsync(new RatingId { Id = id });
            return RedirectToAction(nameof(Index), new { readerId });
        }
        catch (RpcException ex)
        {
            ViewBag.Error = $"{ex.StatusCode}: {ex.Status.Detail}";
            ViewBag.ReaderId = readerId;

            return View("Delete", new RatingItem { Id = id });
        }
    }

}
