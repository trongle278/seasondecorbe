using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface ITrackingService
    {
        Task<BaseResponse<List<Tracking>>> GetTrackingAsync(int bookingId);
        Task AddTrackingAsync(int bookingId, Booking.BookingStatus status, string? note = null, string? imageUrl = null);
        Task<BaseResponse> UpdateTrackingAsync(UpdateTrackingRequest request);
    }
}
