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

    }

    public class GoogleLoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public List<string> Errors { get; set; }
        public bool IsFirstLogin { get; set; }
    }
}
