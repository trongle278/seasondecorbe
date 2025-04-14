using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Subscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; } = 30;
        public bool AutoRenew { get; set; }
        public int VoucherCount { get; set; }
        public int FreeRequestChange { get; set; }
        public bool PrioritySupport { get; set; }
        public double CommissionDiscount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public enum SubscriptionStatus
        {
            Subcribed,
            Unsubcribed
        }
        public SubscriptionStatus Status { get; set; }
        public virtual ICollection<Account> Accounts { get; set; }
        public virtual ICollection<Voucher> Vouchers { get; set; }
    }
}
