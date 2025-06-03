using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IZoomOAuthService
    {
        Task<BaseResponse<ZoomTokenResponse>> GetAccessTokenAsync(string code);
        Task<BaseResponse<ZoomTokenResponse>> RefreshAccessTokenAsync(string refreshToken);
        Task<BaseResponse> AcceptMeetingRequestAsync(string bookingCode, int id, ZoomOAuthRequest request);
        Task<BaseResponse<ZoomSdkJoinInfoResponse>> GetZoomJoinInfo(int id);
        Task<BaseResponse> EndMeetingAsync(string bookingCode, int id, ZoomOAuthRequest request);
    }
}
