using infrastructures.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebsitSellsLaptop.Models;
using WebsitSellsLaptop.Repository.IRepository;
using WebsitSellsLaptop.Utility;

namespace WebsitSellsLaptopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUS : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IContactUs _contactUs;

        public ContactUS(UserManager<ApplicationUser> userManager, IContactUs contactUs)
        {
            this.userManager = userManager;
            this._contactUs = contactUs;
        }

        // ✅ Users can submit a message
        [HttpPost("Create")]
        [Authorize]
        public async Task<IActionResult> CreateContactUs([FromBody] ContactUs contactUs)
        {
            if (string.IsNullOrWhiteSpace(contactUs.Subject) || string.IsNullOrWhiteSpace(contactUs.Message))
            {
                return BadRequest(new { message = "Subject and Message are required." });
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "User is not authenticated." });

            // Auto-fill missing details from authenticated user
            contactUs.UserId = user.Id;
            contactUs.Name = user.UserName ?? "Unknown User";
            contactUs.Email = user.Email ?? "no-email@example.com";
            contactUs.Status = true;

            _contactUs.Create(contactUs);
            _contactUs.Commit();

            return Ok(new { message = "Your message has been sent successfully." });
        }


        // ✅ Admin can view all contact messages
        [HttpGet("AdminMessages")]
        [Authorize(Roles = SD.adminRole)]
        public IActionResult GetContactMessages()
        {
            var messages = _contactUs.Get();
            return Ok(messages);
        }

        // ✅ Users can view their own messages
        [HttpGet("MyMessages")]
        [Authorize]
        public IActionResult GetMyContactMessages()
        {
            var userId = userManager.GetUserId(User);
            var messages = _contactUs.Get(expression: e => e.UserId == userId);
            return Ok(messages);
        }

        // ✅ Users can update their messages
        [HttpPut("Update/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateContactMessage(int id, [FromBody] ContactUs updatedContactUs)
        {
            if (updatedContactUs == null)
                return BadRequest(new { message = "Contact message cannot be null." });

            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "User is not authenticated." });

            var existingMessage = _contactUs.GetOne(expression: e => e.Id == id && e.UserId == user.Id);
            if (existingMessage == null)
                return NotFound(new { message = "Message not found or you do not have permission to edit this message." });
            existingMessage.Subject = updatedContactUs.Subject;
            existingMessage.Message = updatedContactUs.Message;
            existingMessage.UserId = user.Id;
            existingMessage.Name = user.UserName ?? "Unknown User";
            existingMessage.Email = user.Email ?? "no-email@example.com";
            existingMessage.Status = true;

            _contactUs.Edit(existingMessage);
            _contactUs.Commit();

            return Ok(new { message = "Message updated successfully." });
        }


        // ✅ Admin can view a specific contact message
        [HttpGet("Message/{id}")]
        [Authorize]
        public IActionResult GetContactMessage(int id)
        {
            var message = _contactUs.GetOne(expression: e => e.Id == id);
            if (message == null)
                return NotFound(new { message = "Message not found." });

            return Ok(message);
        }

        // ✅ Admin can reply to a message
        [HttpPut("Reply/{id}")]
        [Authorize(Roles = SD.adminRole)]
        public async Task<IActionResult> ReplyToMessage(int id, [FromBody] ContactUs contactUs)
        {
            var contactMessage = _contactUs.GetOne(expression: e => e.Id == id);
            if (contactMessage == null)
                return NotFound(new { message = "Message not found." });

            var adminUser = await userManager.GetUserAsync(User);
            if (adminUser == null)
                return Unauthorized(new { message = "Admin authentication failed." });

            // ✅ Only update the reply fieldexistingMessage.Subject = updatedContactUs.Subject;
        
            contactMessage.Reply = contactUs.Reply;
            contactMessage.Status = false;  // Mark as replied

            _contactUs.Edit(contactMessage);
            _contactUs.Commit();

            return Ok(new { message = "Reply sent successfully." });
        }
        // ✅ Admin can delete a messages
        [HttpDelete("Delete/{id}")]
        [Authorize]
        public IActionResult DeleteContactMessage(int id)
        {
            var message = _contactUs.GetOne( expression:e => e.Id == id);
            if (message == null)
                return NotFound(new { message = "Message not found." });

            _contactUs.Delete(message);
            _contactUs.Commit();

            return Ok(new { message = "Message deleted successfully." });
        }
    }
}
