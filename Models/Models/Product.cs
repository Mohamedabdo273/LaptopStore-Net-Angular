using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebsitSellsLaptop.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ImgUrl { get; set; }
        public int count { get; set; }
        public string Model { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        [BindNever] 
        [ValidateNever]
        public Category Category { get; set; } = null!;
    }
}
