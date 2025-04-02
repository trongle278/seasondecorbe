using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
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

        [HttpGet("getPaginated")]
        public async Task<IActionResult> GetPaginatedDecorService([FromQuery] DecorServiceFilterRequest request)
        {
            var result = await _decorServiceService.GetFilterDecorServicesAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }
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

        [HttpGet("getDecorServiceByProvider/{slug}")]
        public async Task<IActionResult> GetDecorServiceByProvider(string slug)
        {
            var result = await _decorServiceService.GetDecorServiceBySlugAsync(slug);
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

        //[HttpGet("search")]
        //public async Task<IActionResult> Search([FromQuery] string keyword)
        //{
        //    var result = await _decorServiceService.SearchDecorServices(keyword);
        //    if (!result.Success)
        //        return BadRequest(result);
        //    return Ok(result);
        //}

        [HttpGet("search")]
        public async Task<IActionResult> SearchMultiCriteria(
        [FromQuery] string? Style,
        [FromQuery] string? Sublocation,
        [FromQuery] string? CategoryName,
        [FromQuery] List<string>? SeasonNames) // Nhận danh sách thay vì string đơn lẻ
        {
            var request = new SearchDecorServiceRequest
            {
                Style = Style,
                Sublocation = Sublocation,
                CategoryName = CategoryName,
                SeasonNames = SeasonNames
            };

            var result = await _decorServiceService.SearchMultiCriteriaDecorServices(request);
            if (result.Success)
                return Ok(result);
            return BadRequest(result);
        }
        
        [HttpPut("change-startdate/{decorServiceId}")]
        public async Task<IActionResult> ChangeStartDate(int decorServiceId, [FromBody] ChangeStartDateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            var response = await _decorServiceService.ChangeStartDateAsync(decorServiceId, request, accountId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
