using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class LoginResponse
    {
        public string? Token { get; set; }
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
        public bool RequiresTwoFactor { get; set; }

        public LoginResponse()
        {
            Errors = new List<string>();
        }
    }
}
