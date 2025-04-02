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

        public async Task<BaseResponse<List<Tracking>>> GetTrackingAsync(int bookingId)
        {
            var response = new BaseResponse<List<Tracking>>();

            var trackingHistory = await _unitOfWork.TrackingRepository.Queryable()
                .Where(bt => bt.BookingId == bookingId)
                .OrderBy(bt => bt.CreatedAt)
                .ToListAsync();

            if (!trackingHistory.Any())
            {
                response.Message = "No tracking history found for this booking.";
                return response;
            }

            response.Success = true;
            response.Data = trackingHistory;
            return response;
        }

        public async Task AddTrackingAsync(int bookingId, Booking.BookingStatus status, string? note = null)
        {
            var existingTracking = await _unitOfWork.TrackingRepository.Queryable()
                .FirstOrDefaultAsync(bt => bt.BookingId == bookingId && bt.Status == status);

            if (existingTracking == null) // 🔹 Chỉ lưu tracking nếu chưa tồn tại
            {
                var tracking = new Tracking
                {
                    BookingId = bookingId,
                    Status = status,
                    Note = note
                };

                await _unitOfWork.TrackingRepository.InsertAsync(tracking);
                await _unitOfWork.CommitAsync();
            }
        }

        public async Task<BaseResponse> UpdateTrackingAsync(UpdateTrackingRequest request)
        {
            var response = new BaseResponse();

            var booking = await _unitOfWork.BookingRepository.GetByIdAsync(request.BookingId);
            if (booking == null)
            {
                response.Message = "Booking not found.";
                return response;
            }

            // ✅ Kiểm tra Booking có ở giai đoạn Progressing không
            if (booking.Status != Booking.BookingStatus.Progressing)
            {
                response.Message = "Images can only be uploaded for the construction phase.";
                return response;
            }

            // ✅ Tìm Tracking của Booking trong giai đoạn Progressing
            var tracking = await _unitOfWork.TrackingRepository.Queryable()
                .FirstOrDefaultAsync(t => t.BookingId == request.BookingId && t.Status == Booking.BookingStatus.Progressing);

            // ✅ Nếu chưa có Tracking, thì tạo mới
            if (tracking == null)
            {
                tracking = new Tracking
                {
                    BookingId = request.BookingId,
                    Status = Booking.BookingStatus.Progressing,
                    Note = request.Note,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.TrackingRepository.InsertAsync(tracking);
                await _unitOfWork.CommitAsync();
            }
            else
            {
                tracking.Note = request.Note; // Cập nhật ghi chú nếu có
            }

            // ✅ Kiểm tra nếu có ảnh thì upload lên Cloudinary
            if (request.Images != null && request.Images.Any())
            {
                foreach (var imageFile in request.Images)
                {
                    using var stream = imageFile.OpenReadStream();
                    var imageUrl = await _cloudinaryService.UploadFileAsync(
                        stream,
                        $"tracking_{tracking.Id}_{DateTime.UtcNow.Ticks}{Path.GetExtension(imageFile.FileName)}",
                        imageFile.ContentType
                    );

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        tracking.TrackingImages.Add(new TrackingImage { ImageUrl = imageUrl });
                    }
                }
            }

            _unitOfWork.TrackingRepository.Update(tracking);
            await _unitOfWork.CommitAsync();

            response.Success = true;
            response.Message = "Tracking updated successfully.";
            return response;
        }

    }
}
