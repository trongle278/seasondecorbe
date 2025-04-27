using System.Security.Claims;
using BusinessLogicLayer;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Order;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetOrderList()
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _orderService.GetOrderList(accountId);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpGet("getPaginatedList")]
        public async Task<IActionResult> GetFilterOrder([FromQuery] OrderFilterRequest request)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _orderService.GetPaginate(request);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest();
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            var result = await _orderService.GetOrderById(id);

            if (result.Success == false && result.Message == "Invalid order")
            {
                ModelState.AddModelError("", $"Order not found!");
                return StatusCode(404, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving order")
            {
                ModelState.AddModelError("", $"Error retrieving order!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpPost("createOrder/{id}")]
        public async Task<IActionResult> CreateOrder(int id, int addressId)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderService.CreateOrder(id, addressId);

            if (result.Success == false && result.Message == "Invalid cart")
            {
                ModelState.AddModelError("", $"Cart not found!");
                return StatusCode(404, ModelState);
            }
            
            if (result.Success == false && result.Message == "Invalid address")
            {
                ModelState.AddModelError("", $"Address not found!");
                return StatusCode(404, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid item")
            {
                ModelState.AddModelError("", $"Product not found!");
                return StatusCode(404, ModelState);
            }

            if (result.Success == false && result.Message == "Error creating order")
            {
                ModelState.AddModelError("", $"Error creating order!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        //[HttpPut("updateStatus/{id}")]
        //public async Task<IActionResult> UpdateOrderStatus(int id)
        //{
        //    var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        //    if (accountId == 0)
        //    {
        //        return Unauthorized(new { Message = "Unauthorized" });
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var result = await _orderService.UpdateStatus(id);

        //    if (result.Success == false && result.Message == "Invalid order")
        //    {
        //        ModelState.AddModelError("", $"Invalid Order!");
        //        return StatusCode(404, ModelState);
        //    }

        //    if (result.Success == false && result.Message == "Error updating status")
        //    {
        //        ModelState.AddModelError("", $"Error updating status!");
        //        return StatusCode(500, ModelState);
        //    }

        //    return Ok(result);
        //}

        [HttpDelete("cancelOrder/{id}")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderService.CancelOrder(id);

            if (result.Success == false && result.Message == "Invalid order")
            {
                ModelState.AddModelError("", $"Order not found!");
                return StatusCode(404, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid status")
            {
                ModelState.AddModelError("", $"Order cannot be cancelled!");
                return StatusCode(404, ModelState);
            }

            if (result.Success == false && result.Message == "Error cancel order")
            {
                ModelState.AddModelError("", $"Error cancel order!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpPost("payment/{id}")]
        public async Task<IActionResult> ProcessOrderPayment(int id)
        {
            var accountId = GetUserId();
            if (accountId == 0)
            {
                return Unauthorized(new { Message = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderService.ProcessPayment(id);

            if (result.Success == false && result.Message == "Invalid order")
            {
                ModelState.AddModelError("", $"Order not found!");
                return StatusCode(404, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid status")
            {
                ModelState.AddModelError("", $"Order cannot be Paid!");
                return StatusCode(404, ModelState);
            }

            if (result.Success == false && result.Message == "No remaining amount to be paid")
            {
                ModelState.AddModelError("", $"No remaining amount to be paid!");
                return StatusCode(417, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid provider")
            {
                ModelState.AddModelError("", $"Provider not found!");
                return StatusCode(404, ModelState);
            }

            if (result.Success == false && result.Message == "Error process payment")
            {
                ModelState.AddModelError("", $"Error process payment!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }
    }
}
