using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateSupportRequest
    {
        public string Subject { get; set; }
        public string Description { get; set; }
        public int AccountId { get; set; }
        public int TicketTypeId { get; set; }
        public IFormFile[]? Attachments { get; set; }
    }

    public class AddSupportReplyRequest
    {
        public int SupportId { get; set; }
        public int AccountId { get; set; }
        public string Description { get; set; }
        public IFormFile[] Attachments { get; set; }
    }

    public class TicketTypeRequest
    {
        public string Type { get; set; }
    }
}
