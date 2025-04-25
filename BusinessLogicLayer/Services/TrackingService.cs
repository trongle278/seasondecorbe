using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.Interfaces;

namespace BusinessLogicLayer.Services
{
    public class TrackingService: ITrackingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;

        public TrackingService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
        }

        public async Task<BaseResponse<List<TrackingResponse>>> GetTrackingByBookingCodeAsync(string bookingCode)
        {
            var response = new BaseResponse<List<TrackingResponse>>();

            try
            {
                // 🔹 Lấy booking theo BookingCode
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                // 🔹 Lấy tracking liên quan
                var trackingHistory = await _unitOfWork.TrackingRepository.Queryable()
                    .Where(t => t.BookingId == booking.Id)
                    .Include(t => t.TrackingImages)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();

                if (!trackingHistory.Any())
                {
                    response.Message = "No tracking history found for this booking.";
                    return response;
                }

                // 🔹 Map ra DTO
                var trackingResponses = trackingHistory.Select(t => new TrackingResponse
                {
                    BookingCode = t.Booking.BookingCode,
                    Note = t.Note,
                    CreatedAt = t.CreatedAt,
                    ImageUrls = t.TrackingImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>()
                }).ToList();

                response.Success = true;
                response.Data = trackingResponses;
                response.Message = "Tracking history retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to retrieve tracking history.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }


        //public async Task AddTrackingAsync(int bookingId, Booking.BookingStatus status, string? note = null)
        //{
        //    var existingTracking = await _unitOfWork.TrackingRepository.Queryable()
        //        .FirstOrDefaultAsync(bt => bt.BookingId == bookingId && bt.Status == status);

        //    if (existingTracking == null) // 🔹 Chỉ lưu tracking nếu chưa tồn tại
        //    {
        //        var tracking = new Tracking
        //        {
        //            BookingId = bookingId,
        //            Status = status,
        //            Note = note
        //        };

        //        await _unitOfWork.TrackingRepository.InsertAsync(tracking);
        //        await _unitOfWork.CommitAsync();
        //    }
        //}

        public async Task<BaseResponse> UpdateTrackingAsync(UpdateTrackingRequest request, string bookingCode)
        {
            var response = new BaseResponse();

            try
            {
                // 🔹 Lấy Booking theo BookingCode
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                // 🔹 Kiểm tra Booking phải đang ở giai đoạn Progressing
                if (booking.Status != Booking.BookingStatus.Progressing)
                {
                    response.Message = "Tracking can be updated during the Progressing phase.";
                    return response;
                }

                // 🔹 Tạo mới một bản ghi Tracking mới cho lần upload này
                var tracking = new Tracking
                {
                    BookingId = booking.Id,
                    Note = request.Note,
                    CreatedAt = DateTime.Now,
                    TrackingImages = new List<TrackingImage>()
                };

                // 🔹 Nếu có hình ảnh upload
                if (request.Images != null && request.Images.Any())
                {
                    // Kiểm tra số lượng ảnh tải lên tối đa là 5
                    if (request.Images.Count() > 5)
                    {
                        response.Message = "You can upload a maximum of 5 images.";
                        return response;
                    }

                    foreach (var imageFile in request.Images)
                    {
                        using var stream = imageFile.OpenReadStream();
                        var imageUrl = await _cloudinaryService.UploadFileAsync(
                            stream,
                            $"tracking_{booking.BookingCode}_{DateTime.Now.Ticks}{Path.GetExtension(imageFile.FileName)}",
                            imageFile.ContentType
                        );

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            tracking.TrackingImages.Add(new TrackingImage { ImageUrl = imageUrl });
                        }
                    }
                }

                // 🔹 Lưu tracking mới vào database
                booking.IsTracked = true;
                await _unitOfWork.TrackingRepository.InsertAsync(tracking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Tracking updated successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update tracking.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
