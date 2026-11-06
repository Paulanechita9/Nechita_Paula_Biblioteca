
using System;
using System.ComponentModel.DataAnnotations;

namespace Intalnirea1.Models
{
    public class Rating
    {
        public int ID { get; set; }

        public int ReaderId { get; set; }

        public int BookID { get; set; }

        public Book Book { get; set; } = null!;

        [Range(1, 5)]
        public int Stars { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
