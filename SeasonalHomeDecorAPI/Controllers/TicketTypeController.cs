using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketTypeController : ControllerBase
    {
        private readonly ITicketTypeService _ticketTypeService;

        public TicketTypeController(ITicketTypeService ticketTypeService)
        {
            _ticketTypeService = ticketTypeService;
        }

        // GET: api/TicketType
        [HttpGet]
        public async Task<IActionResult> GetAllTicketTypes()
        {
            var response = await _ticketTypeService.GetAllTicketTypesAsync();
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // GET: api/TicketType/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicketTypeById(int id)
        {
            var response = await _ticketTypeService.GetTicketTypeByIdAsync(id);
            if (response.Success)
            {
                return Ok(response);
            }
            return NotFound(response);
        }

        // POST: api/TicketType
        [HttpPost]
        public async Task<IActionResult> CreateTicketType([FromBody] TicketTypeRequest request)
        {
            if (request == null)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Invalid ticket type request"
                });
            }

            var response = await _ticketTypeService.CreateTicketTypeAsync(request);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // PUT: api/TicketType/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicketType(int id, [FromBody] TicketTypeRequest request)
        {
            if (request == null)
            {
                return BadRequest(new BaseResponse
                {
                    Success = false,
                    Message = "Invalid ticket type request"
                });
            }

            var response = await _ticketTypeService.UpdateTicketTypeAsync(id, request);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // DELETE: api/TicketType/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicketType(int id)
        {
            var response = await _ticketTypeService.DeleteTicketTypeAsync(id);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
