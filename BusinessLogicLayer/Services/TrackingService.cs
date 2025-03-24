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

        public async Task AddTrackingAsync(int bookingId, Booking.BookingStatus status, string? note = null, string? imageUrl = null)
        {
            var existingTracking = await _unitOfWork.TrackingRepository.Queryable()
                .FirstOrDefaultAsync(bt => bt.BookingId == bookingId && bt.Status == status);

            if (existingTracking == null) // 🔹 Chỉ lưu tracking nếu chưa tồn tại
            {
                var tracking = new Tracking
                {
                    BookingId = bookingId,
                    Status = status,
                    Note = note,
                    ImageUrl = imageUrl
                };

                await _unitOfWork.TrackingRepository.InsertAsync(tracking);
                await _unitOfWork.CommitAsync();
            }
        }

        public async Task<BaseResponse> UpdateTrackingAsync(UpdateTrackingRequest request)
        {
            var response = new BaseResponse();

            var tracking = await _unitOfWork.TrackingRepository.GetByIdAsync(request.TrackingId);
            if (tracking == null)
            {
                response.Message = "Tracking entry not found.";
                return response;
            }

            tracking.Note = request.Note;
            tracking.ImageUrl = request.ImageUrl;
            _unitOfWork.TrackingRepository.Update(tracking);
            await _unitOfWork.CommitAsync();

            response.Success = true;
            response.Message = "Tracking updated successfully.";
            return response;
        }
    }
}
