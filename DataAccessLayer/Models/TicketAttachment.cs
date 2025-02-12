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

        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public long FileData {  get; set; }
        public DateTime UploadTime { get; set; }

        public int SupportId { get; set; }
        public Support Support { get; set; }

        public int TicketReplyId { get; set; }
        public TicketReply TicketReply { get; set; }
    }
}
