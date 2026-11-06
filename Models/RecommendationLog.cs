
using System.ComponentModel.DataAnnotations;

namespace Intalnirea1.Models;

public class RecommendationLog
{
    public int ID { get; set; }

    public int ReaderId { get; set; }

    public int BookID { get; set; }

    public Book Book { get; set; } = null!;

    public double Score { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
