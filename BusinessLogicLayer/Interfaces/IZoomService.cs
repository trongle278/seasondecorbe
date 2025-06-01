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
        Task<BaseResponse<PageResult<MeetingListResponse>>> GetMeetingForCustomerAsync(int accountId, ZoomFilterRequest request);
        Task<BaseResponse<PageResult<MeetingListResponse>>> GetMeetingForProviderAsync(int accountId, ZoomFilterRequest request);
        Task<BaseResponse<MeetingDetailResponse>> GetMeetingById (int id);
        Task<BaseResponse<PageResult<MeetingScheduleReponse>>> GetProviderMeetingsForCustomerAsync(ZoomFilterRequest request);
        Task<BaseResponse> CreateMeetingRequestAsync(string bookingCode, int customerId, CreateMeetingRequest request);
        Task<BaseResponse> AcceptMeetingRequestAsync(string bookingCode, int id);
        Task<BaseResponse> RejectMeetingRequestAsync(string bookingCode, int id);
        Task<BaseResponse> EndMeetingAsync(string bookingCode, int id);
        Task<BaseResponse> CreateMeetingScheduleAsync(string bookingCode, int providerId, List<DateTime> scheduledTime);
        Task<BaseResponse> SelectMeetingAsync(string bookingCode, int id);
        Task<BaseResponse> CancelMeetingAsync(int id);
        Task<ZoomMeetingResponse> CreateMeetingAsync(ZoomMeetingRequest request);
        //Task<BaseResponse<ZoomJoinInfoResponse>> GetZoomJoinInfo(int id);
    }
}
