
using System;
using System.ComponentModel.DataAnnotations;

namespace Intalnirea1.Models
{
    public class OrderCreateViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selectează un cititor.")]
        public int CustomerID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selectează o carte.")]
        public int BookID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;
    }
}
