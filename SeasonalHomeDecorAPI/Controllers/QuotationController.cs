﻿using System.Security.Claims;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeasonalHomeDecorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuotationController : ControllerBase
    {
        private readonly IQuotationService _quotationService;

        public QuotationController(IQuotationService quotationService)
        {
            _quotationService = quotationService;
        }

        /// <summary>
        /// Tạo báo giá cho một booking
        /// </summary>
        [HttpPost("createQuotationByBookingCode/{bookingCode}")]
        public async Task<IActionResult> CreateQuotation(string bookingCode, CreateQuotationRequest request)
        {
            var response = await _quotationService.CreateQuotationAsync(bookingCode, request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("uploadQuotationFileByBookingCode/{bookingCode}")]
        public async Task<IActionResult> UploadQuotationFile(string bookingCode, IFormFile quotationFile)
        {
            var response = await _quotationService.UploadQuotationFileAsync(bookingCode, quotationFile);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getQuotationByBookingId/{bookingCode}")]
        public async Task<IActionResult> GetQuotation(string bookingCode)
        {
            var response = await _quotationService.GetQuotationByBookingCodeAsync(bookingCode);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("getPaginatedQuotationsForCustomer")]
        public async Task<IActionResult> GetPaginatedQuotationsForCustomerAsync([FromQuery] QuotationFilterRequest request)
        {
            // Lấy accountId từ token
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _quotationService.GetPaginatedQuotationsForCustomerAsync(request, accountId);
            return Ok(result);
        }

        [HttpGet("getPaginatedQuotationsForProvider")]
        public async Task<IActionResult> GetPaginatedQuotationsForProviderAsync([FromQuery] QuotationFilterRequest request)
        {
            // Lấy accountId từ token
            var providerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _quotationService.GetPaginatedQuotationsForCustomerAsync(request, providerId);
            return Ok(result);
        }
    }
}
