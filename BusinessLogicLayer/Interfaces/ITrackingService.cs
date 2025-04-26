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
        Task<BaseResponse<List<TrackingResponse>>> GetTrackingByBookingCodeAsync(string bookingCode);
        Task<BaseResponse> AddTrackingAsync(TrackingRequest request, string bookingCode);
        Task<BaseResponse> UpdateTrackingAsync(UpdateTrackingRequest request, int trackingId);
        Task<BaseResponse> RemoveTrackingAsync(int trackingId);
    }
}
