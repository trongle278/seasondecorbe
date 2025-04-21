using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using static DataAccessObject.Models.Support;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateSupportRequest
    {
        public string Subject { get; set; }
        public string Description { get; set; }
        public int TicketTypeId { get; set; }
        public int BookingId { get; set; }
        public IFormFile[]? Attachments { get; set; }
    }

    public class AddSupportReplyRequest
    {
        public string Description { get; set; }
        public IFormFile[]? Attachments { get; set; }
    }

    public class TicketTypeRequest
    {
        public string Type { get; set; }
    }

    public class SupportFilterRequest
    {
        public TicketStatusEnum? TicketStatus { get; set; }
        public int? BookingId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreateAt";
        public bool Descending { get; set; } = false;
    }
}
