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
        public bool? IsSolved { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }


        public int TicketTypeId { get; set; }
        public TicketType TicketType { get; set; }

        public virtual ICollection<TicketReply> TicketReplies { get; set; }
        public virtual ICollection<TicketAttachment> TicketAttachments { get; set; }
    }
}
