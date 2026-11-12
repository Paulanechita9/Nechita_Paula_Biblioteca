
using BibliotecaGrpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Intalnirea1.Data;
using Intalnirea1.Models;
using Microsoft.EntityFrameworkCore;

namespace Intalnirea1.Services;

public class ReaderRatingsGrpcService(LibraryContext db)
    : ReaderRatings.ReaderRatingsBase
{
    private static RatingItem ToItem(Rating r) => new()
    {
        Id = r.ID,
        BookId = r.BookID,
        Title = r.Book?.Title ?? "",
        Stars = r.Stars,
        CreatedAt = Timestamp.FromDateTime(
            DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc))
    };

    public override async Task ListRatings(
        ReaderRequest request,
        IServerStreamWriter<RatingItem> responseStream,
        ServerCallContext context)
    {
        if (request.ReaderId <= 0)
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "ID-ul cititorului trebuie să fie pozitiv."));

        var ratings = await db.Rating
            .AsNoTracking()
            .Include(r => r.Book)
            .Where(r => r.ReaderId == request.ReaderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(context.CancellationToken);

        foreach (var rating in ratings)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await responseStream.WriteAsync(ToItem(rating));
        }
    }

    public override async Task<RatingItem> GetRating(
        RatingId request, ServerCallContext context)
    {
        var rating = await db.Rating
            .AsNoTracking()
            .Include(r => r.Book)
            .FirstOrDefaultAsync(
                r => r.ID == request.Id,
                context.CancellationToken);

        if (rating == null)
            throw new RpcException(new Status(
                StatusCode.NotFound,
                "Evaluarea solicitată nu există."));

        return ToItem(rating);
    }

    public override async Task<RatingItem> AddRating(
        NewRating request, ServerCallContext context)
    {
        if (request.ReaderId <= 0)
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "ID-ul cititorului trebuie să fie pozitiv."));

        if (request.Stars < 1 || request.Stars > 5)
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Nota trebuie să fie între 1 și 5."));

        var book = await db.Book.FindAsync(
            new object[] { request.BookId },
            context.CancellationToken);

        if (book == null)
            throw new RpcException(new Status(
                StatusCode.NotFound,
                "Cartea solicitată nu există."));

        var rating = new Rating
        {
            ReaderId = request.ReaderId,
            BookID = request.BookId,
            Book = book,
            Stars = request.Stars,
            CreatedAt = DateTime.UtcNow
        };

        db.Rating.Add(rating);
        await db.SaveChangesAsync(context.CancellationToken);

        return ToItem(rating);
    }

    public override async Task<RatingItem> UpdateRating(
        UpdateRatingRequest request,
        ServerCallContext context)
    {
        if (request.Stars < 1 || request.Stars > 5)
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "Nota trebuie să fie între 1 și 5."));

        var rating = await db.Rating
            .Include(r => r.Book)
            .FirstOrDefaultAsync(
                r => r.ID == request.Id,
                context.CancellationToken);

        if (rating == null)
            throw new RpcException(new Status(
                StatusCode.NotFound,
                "Evaluarea pe care doriți să o modificați nu există."));

        rating.Stars = request.Stars;

        await db.SaveChangesAsync(context.CancellationToken);

        return ToItem(rating);
    }

    public override async Task<Empty> DeleteRating(
        RatingId request, ServerCallContext context)
    {
        var rating = await db.Rating.FindAsync(
            new object[] { request.Id },
            context.CancellationToken);

        if (rating == null)
            throw new RpcException(new Status(
                StatusCode.NotFound,
                "Evaluarea pe care doriți să o ștergeți nu există."));

        db.Rating.Remove(rating);

        await db.SaveChangesAsync(context.CancellationToken);

        return new Empty();
    }
}
