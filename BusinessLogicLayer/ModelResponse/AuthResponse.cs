using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
        public AuthResponse()
        {
            Errors = new List<string>();
            Token = string.Empty;         
        }

    }
    public class LoginResponse
    {
        public string? Token { get; set; }
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
        public int AccountId { get; set; }
        public int? SubscriptionId { get; set; }
        public int RoleId { get; set; }
        public bool RequiresTwoFactor { get; set; }

        public LoginResponse()
        {
            Errors = new List<string>();
        }
    }
    public class GoogleLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public int RoleId { get; set; }
        public int AccountId { get; set; }
        public int? SubscriptionId { get; set; }
        public List<string> Errors { get; set; }
    }
    public class ForgotPasswordResponse : BaseResponse
    {
        public ForgotPasswordResponse()
        {
            Success = false;
            Message = string.Empty;
            Errors = new List<string>();
        }
    }
    public class Toggle2FAResponse
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
        public bool TwoFactorEnabled { get; set; }

        public Toggle2FAResponse()
        {
            Errors = new List<string>();
        }
    }
}
