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
            Errors = new List<string>();  // Khởi tạo list trong constructor
            Token = string.Empty;         // Khởi tạo token rỗng
        }

    }

    public class GoogleLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public List<string> Errors { get; set; }
        public bool IsFirstLogin { get; set; }
    }
}
