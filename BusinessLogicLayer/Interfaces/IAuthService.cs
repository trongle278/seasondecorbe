using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

namespace BusinessLogicLayer.Interfaces
{
    public interface IAuthService
    {
        Task<BaseResponse> RegisterCustomerAsync(RegisterRequest request);
        Task<BaseResponse> VerifyEmailAsync(VerifyEmailRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> VerifyLoginOTPAsync(VerifyOtpRequest request);
        Task<GoogleLoginResponse> GoogleLoginAsync(string idToken);
        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<Toggle2FAResponse> Toggle2FAAsync(int userId, Toggle2FARequest request);
    }
}

