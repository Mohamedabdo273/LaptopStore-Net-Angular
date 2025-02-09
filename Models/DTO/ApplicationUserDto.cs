using System.ComponentModel.DataAnnotations;

namespace WebsitSellsLaptop.DTO
{
    public class ApplicationUserDto
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Passwords { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Passwords))]
        public string ConfirmPassword { get; set; }
        
    }
}
