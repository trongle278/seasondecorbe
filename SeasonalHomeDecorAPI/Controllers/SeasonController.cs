using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeasonController : ControllerBase
    {
        private readonly ISeasonService _seasonService;

        public SeasonController(ISeasonService seasonService)
        {
            _seasonService = seasonService;
        }

        // GET: api/season
        [HttpGet]
        public async Task<IActionResult> GetAllSeasons()
        {
            var response = await _seasonService.GetAllSeasonsAsync();
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        // GET: api/season/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSeasonById(int id)
        {
            var response = await _seasonService.GetSeasonByIdAsync(id);
            if (response.Success)
                return Ok(response);
            return NotFound(response);
        }

        // POST: api/season
        [HttpPost]
        public async Task<IActionResult> CreateSeason([FromBody] SeasonRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _seasonService.CreateSeasonAsync(request);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        // PUT: api/season/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSeason(int id, [FromBody] SeasonRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _seasonService.UpdateSeasonAsync(id, request);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        // DELETE: api/season/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSeason(int id)
        {
            var response = await _seasonService.DeleteSeasonAsync(id);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }
    }
}
