﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class SupportResponse
    {
        public int Id { get; set; }
        public string BookingCode { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public DateTime CreateAt { get; set; }
        public bool? IsSolved { get; set; }
        public string TicketType { get; set; }
        // Danh sách các reply của ticket
        public List<string> AttachmentUrls { get; set; }
        public List<SupportReplyResponse> Replies { get; set; }
        // Danh sách URL (hoặc path) của file đính kèm
    }

    public class ProviderSupportPaginateResponse
    {
        public int Id { get; set; }
        public string BookingCode { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public DateTime CreateAt { get; set; }
        public bool? IsSolved { get; set; }  
        public string TicketType { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public List<string> AttachmentUrls { get; set; }
        public List<SupportReplyResponse> Replies { get; set; }     
    }

    public class SupportReplyResponse
    {
        public int Id { get; set; }
        public string AccountName { get; set; }
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
