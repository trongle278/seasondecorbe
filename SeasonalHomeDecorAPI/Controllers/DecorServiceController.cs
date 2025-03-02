using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]  
    public class DecorServiceController : ControllerBase
    {
        private readonly IDecorServiceService _decorServiceService;

        public DecorServiceController(IDecorServiceService decorServiceService)
        {
            _decorServiceService = decorServiceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDecorServices()
        {
            var result = await _decorServiceService.GetAllDecorServicesAsync();
            if (result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDecorService(int id)
        {
            var result = await _decorServiceService.GetDecorServiceByIdAsync(id);
            if (result.Success)
                return Ok(result);
            return NotFound(result.Message);
        }

        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> CreateDecorService([FromForm] CreateDecorServiceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Lấy accountId từ token
            int accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var result = await _decorServiceService.CreateDecorServiceAsync(request, accountId);
            if (result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateDecorService(int id, [FromForm] UpdateDecorServiceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Lấy accountId từ token (nếu có)
            int accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var result = await _decorServiceService.UpdateDecorServiceAsync(id, request, accountId);
            if (result.Success)
                return Ok(result);

            return BadRequest(result.Message);
        }

        //[HttpPut("{id}")]
        //[Consumes("multipart/form-data")]
        //public async Task<IActionResult> UpdateDecorServiceWithImage(int id, [FromForm] UpdateDecorServiceRequest request)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    // Lấy accountId từ token (nếu có)
        //    int accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

        //    var result = await _decorServiceService.UpdateDecorServiceAsyncWithImage(id, request, accountId);
        //    if (result.Success)
        //        return Ok(result);

        //    return BadRequest(result.Message);
        //}

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteDecorService(int id, int accountId)
        {
            var result = await _decorServiceService.DeleteDecorServiceAsync(id, accountId);
            if (result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            var result = await _decorServiceService.SearchDecorServices(keyword);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
