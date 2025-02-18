using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class SupportResponse : BaseResponse
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public DateTime CreateAt { get; set; }
        public string Status { get; set; }
        public int TicketTypeId { get; set; }
        public int AccountId { get; set; }
        // Danh sách các reply của ticket
        public List<SupportReplyResponse> Replies { get; set; }
        // Danh sách URL (hoặc path) của file đính kèm
        public List<string> AttachmentUrls { get; set; }
    }

    public class SupportReplyResponse
    {
        public int Id { get; set; }
        public int SupportId { get; set; }
        public int AccountId { get; set; }
        public string Description { get; set; }
        public DateTime CreateAt { get; set; }
        public List<string> AttachmentUrls { get; set; }
    }

    public class TicketTypeResponse
    {
        public int Id { get; set; }
        public string Type { get; set; }
    }
}
