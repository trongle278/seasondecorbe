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
            return Ok(response);
        }

        [HttpPost("add/{receiverId}")]
        public async Task<IActionResult> AddToContactList(int receiverId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var response = await _contactService.AddToContactListAsync(userId, receiverId);
            return Ok(response);
        }
    }
}
