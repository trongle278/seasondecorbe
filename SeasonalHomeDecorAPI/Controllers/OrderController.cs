using BusinessLogicLayer;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Order;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("getList")]
        public async Task<IActionResult> GetOrderList()
        {
            var result = await _orderService.GetOrderList();

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var result = await _orderService.GetOrderById(id);

            if (result.Success == false && result.Message == "Invalid order")
            {
                ModelState.AddModelError("", $"Order not found!");
                return StatusCode(400, ModelState);
            }

            if (result.Success == false && result.Message == "Error retrieving order")
            {
                ModelState.AddModelError("", $"Error retrieving order!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpPost("createOrder/{id}")]
        //[Authorize(Policy = "RequireCustomerRole")]
        public async Task<IActionResult> CreateOrder(int id, int addressId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderService.CreateOrder(id, addressId);

            if (result.Success == false && result.Message == "Invalid cart")
            {
                ModelState.AddModelError("", $"Cart not found!");
                return StatusCode(403, ModelState);
            }
            
            if (result.Success == false && result.Message == "Invalid address")
            {
                ModelState.AddModelError("", $"Address not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid item")
            {
                ModelState.AddModelError("", $"Product not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error creating order")
            {
                ModelState.AddModelError("", $"Error creating order!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpPut("updateStatus/{id}")]
        //[Authorize(Policy = "RequireCustomerRole")]
        public async Task<IActionResult> UpdateOrderStatus(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderService.UpdateStatus(id);

            if (result.Success == false && result.Message == "Invalid order")
            {
                ModelState.AddModelError("", $"Invalid Order!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Error updating status")
            {
                ModelState.AddModelError("", $"Error updating status!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }

        [HttpDelete("cancelOrder/{id}")]
        //[Authorize(Policy = "RequireCustomerRole")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderService.CancelOrder(id);

            if (result.Success == false && result.Message == "Invalid order")
            {
                ModelState.AddModelError("", $"Order not found!");
                return StatusCode(403, ModelState);
            }

            if (result.Success == false && result.Message == "Invalid status")
            {
                ModelState.AddModelError("", $"Order cannot be cancelled!");
            }

            if (result.Success == false && result.Message == "Error cancel order")
            {
                ModelState.AddModelError("", $"Error cancel order!");
                return StatusCode(500, ModelState);
            }

            return Ok(result);
        }
    }
}
