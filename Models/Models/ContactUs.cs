using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebsitSellsLaptop.Models
{
    public class ContactUs
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public string? Reply { get; set; } // ✅ Admin's reply message
        public bool Status { get; set; }
    }

}
