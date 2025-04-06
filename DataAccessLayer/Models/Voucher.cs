using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Voucher
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string VoucherName { get; set; }
        public string OfferCode { get; set; }
        public decimal Discount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public enum DiscountType
        {
            Percentage,
            Amount
        }
        public DiscountType Type { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public enum VoucherStatus
        {
            Valid,
            Invalid,
            Expired
        }
        public VoucherStatus Status { get; set; }

        public int? SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
    }
}
