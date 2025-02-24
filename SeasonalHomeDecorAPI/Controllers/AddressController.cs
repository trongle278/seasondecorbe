using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        private int GetUserAccountId()
        {
            // ClaimTypes.NameIdentifier thường được gắn = accountId khi login
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        // GET: api/Address
        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            try
            {
                var userAccountId = GetUserAccountId();
                var response = await _addressService.GetAddressesAsync(userAccountId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/Address
        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] AddressRequest request)
        {
            try
            {
                var userAccountId = GetUserAccountId();
                var response = await _addressService.CreateAddressAsync(request, userAccountId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Address/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] AddressRequest request)
        {
            try
            {
                var userAccountId = GetUserAccountId();
                var response = await _addressService.UpdateAddressAsync(id, request, userAccountId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/Address/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                var userAccountId = GetUserAccountId();
                var response = await _addressService.DeleteAddressAsync(id, userAccountId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/Address/set-default/{id}
        [HttpPost("set-default/{id}")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            try
            {
                var userAccountId = GetUserAccountId();
                var response = await _addressService.SetDefaultAddressAsync(id, userAccountId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
