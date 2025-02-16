using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Support
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Subject { get; set; }
        public string Description { get; set; }
        public DateTime CreateAt { get; set; }

        // Định nghĩa enum cho trạng thái ticket
        public enum TicketStatusEnum
        {
            Pending,
            Solved,
            Cancelled
        }

        // Property lưu trạng thái, đặt tên là TicketStatus
        public TicketStatusEnum TicketStatus { get; set; }

        // Khóa ngoại và các navigation property
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int TicketTypeId { get; set; }
        public TicketType TicketType { get; set; }

        public virtual ICollection<TicketReply> TicketReplies { get; set; }
        public virtual ICollection<TicketAttachment> TicketAttachments { get; set; }
    }
}
