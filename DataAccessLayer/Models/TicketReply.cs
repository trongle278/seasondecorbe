using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class TicketReply
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Description { get; set; }
        public DateTime CreateAt { get; set; }

        public int SupportId { get; set; }
        public Support Support { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public virtual ICollection<TicketAttachment> TicketAttachments { get; set; }
    }
}
