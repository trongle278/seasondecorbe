﻿using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Interfaces;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.CreateBookingAsync(request, accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getBookingRequestList")]
        public async Task<IActionResult> GetBookingsByUser()
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.GetBookingsByUserAsync(accountId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getBookingDetails/{bookingId}")]
        public async Task<IActionResult> GetBookingDetails(int bookingId)
        {
            var response = await _bookingService.GetBookingDetailsAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("status/{bookingId}")]
        public async Task<IActionResult> ChangeBookingStatus(int bookingId)
        {
            var response = await _bookingService.ChangeBookingStatusAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("cancel/{bookingId}")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var response = await _bookingService.CancelBookingAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("reject/{bookingId}")]
        public async Task<IActionResult> RejectBooking(int bookingId, [FromBody] RejectBookingRequest request)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var response = await _bookingService.RejectBookingAsync(bookingId, accountId, request.Reason);
            return response.Success ? Ok(response) : BadRequest(response);
        }


        [HttpPost("deposit/{bookingId}")]
        public async Task<IActionResult> ProcessDeposit(int bookingId)
        {
            var response = await _bookingService.ProcessDepositAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("payment/{bookingId}")]
        public async Task<IActionResult> ProcessConstructionPayment(int bookingId)
        {
            var response = await _bookingService.ProcessFinalPaymentAsync(bookingId);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
