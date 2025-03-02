using System.Security.Claims;
using BusinessLogicLayer;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class AccountManagementController : ControllerBase
    {
        private readonly IAccountManagementService _accountManagementService;

        public AccountManagementController(IAccountManagementService accountManagementService)
        {
            _accountManagementService = accountManagementService;
        }

        // Lấy danh sách tất cả các tài khoản
        [HttpGet]
        public async Task<IActionResult> GetAllAccounts()
        {
            var result = await _accountManagementService.GetAllAccountsAsync();
            return Ok(result);
        }

        // Lấy thông tin tài khoản theo id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            var result = await _accountManagementService.GetAccountByIdAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return NotFound(result);
        }

        // Tạo tài khoản mới
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _accountManagementService.CreateAccountAsync(request);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        // Cập nhật thông tin tài khoản
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _accountManagementService.UpdateAccountAsync(id, request);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        // Ban tài khoản (set isDisable = true)
        [HttpPut("ban/{id}")]
        public async Task<IActionResult> BanAccount(int id)
        {
            var result = await _accountManagementService.BanAccountAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        // Nếu cần mở khóa tài khoản (set isDisable = false)
        [HttpPut("unban/{id}")]
        public async Task<IActionResult> UnbanAccount(int id)
        {
            var result = await _accountManagementService.UnbanAccountAsync(id);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
