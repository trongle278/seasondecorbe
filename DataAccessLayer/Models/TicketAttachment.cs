using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class TicketAttachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Liên kết với ticket (Support)
        public int SupportId { get; set; }
        public Support Support { get; set; }

        // Nếu file đính kèm thuộc về reply thì cho phép null (nếu thuộc ticket chính)
        public int? TicketReplyId { get; set; }
        public TicketReply TicketReply { get; set; }

        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
