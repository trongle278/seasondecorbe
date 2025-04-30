using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingController : ControllerBase
    {
        private ISettingService _settingService;

        public SettingController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [Authorize]
        [HttpGet("getList")]
        public async Task<IActionResult> GetSettingList([FromQuery] SettingFilterRequest request)
        {
            var account = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (account == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _settingService.GetSettingAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetSettingById(int id)
        {
            var account = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (account == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _settingService.GetSettingByIdAsync(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [Authorize]
        [HttpPut("updateSetting")]
        public async Task<IActionResult> UpdateSetting(int id, SettingRequest request)
        {
            var account = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (account == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _settingService.UpdateSettingAsync(id, request);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
