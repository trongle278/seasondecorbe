using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class ForgotPasswordResponse : BaseResponse
    {
        public ForgotPasswordResponse()
        {
            Success = false;
            Message = string.Empty;
            Errors = new List<string>();
        }
    }
}
