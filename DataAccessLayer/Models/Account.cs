using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Account
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Gender { get; set; }
        public string? Phone {  get; set; }
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public string? Status { get; set; }
        public bool IsDisable { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorToken { get; set; }
        public DateTime? TwoFactorTokenExpiry { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        public virtual Decorator Decorator { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Follower> Followers { get; set; }
        public virtual ICollection<Support> Supports { get; set; }
        public virtual ICollection<TicketReply> TicketReplies { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
    }
}
