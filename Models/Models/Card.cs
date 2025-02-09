using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace WebsitSellsLaptop.Models
{
    public class Card
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [ValidateNever]
        public Product product { get; set; }
        public string UserId { get; set; }
        [ValidateNever]
        public ApplicationUser ApplicationUsers { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Count must be 0 or more.")]
        public int count { get; set; }
    }
}
