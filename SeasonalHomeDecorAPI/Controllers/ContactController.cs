using BusinessLogicLayer.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using Microsoft.AspNetCore.Authorization;
using BusinessLogicLayer.ModelResponse;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/contact")]
    [ApiController]
    [Authorize]
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> GetAllContacts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var response = await _contactService.GetAllContactsAsync(userId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            // Ép kiểu Data về danh sách ContactResponse trước khi Select
            var formattedContacts = ((List<ContactResponse>)response.Data).Select(c => new
            {
                c.ContactId,
                c.ContactName,
                c.Avatar,
                c.Message,
                LastMessageTime = c.LastMessageTime.ToString("dd/MM/yy")
            });

            return Ok(formattedContacts);
        }

        [HttpPost("add-to-contact-list")]
        public async Task<IActionResult> AddToContactList([FromBody] CreateContactRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _contactService.AddToContactListAsync(userId, request.ReceiverId);

            return Ok(new { message = "User added to contact list." });
        }
    }
}
