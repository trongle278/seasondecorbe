using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Chat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Message { get; set; }
        public DateTime SentTime { get; set; }
        public bool IsRead { get; set; }

        [ForeignKey("SenderId")]
        public virtual Account Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual Account Receiver { get; set; }
        public virtual ICollection<ChatFile> ChatFiles { get; set; } = new List<ChatFile>();
    }
}
