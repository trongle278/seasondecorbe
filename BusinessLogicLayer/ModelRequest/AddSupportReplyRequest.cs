using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelRequest
{
    public class AddSupportReplyRequest
    {
        public int SupportId { get; set; }
        public int AccountId { get; set; }
        public string Description { get; set; }
        public IFormFile[] Attachments { get; set; }
    }
}
