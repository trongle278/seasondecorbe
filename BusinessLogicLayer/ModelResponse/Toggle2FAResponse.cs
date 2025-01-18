using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
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
