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
        public string? Slug { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? Gender { get; set; }
        public string? Phone {  get; set; }
        public string? Avatar { get; set; }
        public string? Status { get; set; }
        public bool IsDisable { get; set; }
        public bool IsVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpiry { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorToken { get; set; }
        public DateTime? TwoFactorTokenExpiry { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        public int? SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }

        public Provider Provider { get; set; }
        public Cart Cart { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Follow> Followers { get; set; }
        public virtual ICollection<Follow> Followings { get; set; }
        public virtual ICollection<Support> Supports { get; set; }
        public virtual ICollection<TicketReply> TicketReplies { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Address> Addresses { get; set; }= new List<Address>();
        //test
        public virtual ICollection<DecorService> DecorServices { get; set; }
        public ICollection<DeviceToken> DeviceTokens { get; set; }
        public virtual Wallet Wallet { get; set; }
    }
}
