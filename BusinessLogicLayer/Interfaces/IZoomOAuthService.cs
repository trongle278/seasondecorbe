using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IZoomOAuthService
    {
        BaseResponse<string> GenerateZoomAuthorizeUrl();
        Task<BaseResponse<ZoomTokenResponse>> ExchangeCodeForTokenAsync(string code);
    }
}
