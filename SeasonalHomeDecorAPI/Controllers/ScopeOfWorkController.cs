using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScopeOfWorkController : ControllerBase
    {
        private readonly IScopeOfWorkService _scopeOfWorkService;

        public ScopeOfWorkController(IScopeOfWorkService scopeOfWorkService)
        {
            _scopeOfWorkService = scopeOfWorkService;
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetAllScopeOfWork()
        {
            var result = await _scopeOfWorkService.GetScopeOfWork();

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetScopeOfWorkById(int id)
        {
            var result = await _scopeOfWorkService.GetScopeOfWorkById(id);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
