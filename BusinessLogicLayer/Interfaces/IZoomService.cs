using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;

namespace BusinessLogicLayer.Interfaces
{
    public interface IZoomService
    {
        Task<BaseResponse<PageResult<MeetingListResponse>>> GetMeetingByBookingAsync(int accountId, ZoomFilterRequest request);
        Task<BaseResponse<MeetingDetailResponse>> GetMeetingById (int meetingId);
        Task<BaseResponse> CreateMeetingRequestAsync(string bookingCode, CreateMeetingRequest request);
        Task<BaseResponse> AcceptMeetingRequestAsync(string bookingCode);
        Task<BaseResponse> RejectMeetingRequestAsync(string bookingCode);
        Task<ZoomMeetingResponse> CreateMeetingAsync(ZoomMeetingRequest request);
    }
}
