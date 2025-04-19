using BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CancelTypeController : ControllerBase
    {
        private readonly ICancelTypeService _cancelTypeService;

        public CancelTypeController(ICancelTypeService cancelTypeService)
        {
            _cancelTypeService = cancelTypeService;
        }

        [HttpGet("getAllCancelType")]
        public async Task<IActionResult> GetAllCancelType()
        {
            var result = await _cancelTypeService.GetAllCancelTypeAsync();
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
