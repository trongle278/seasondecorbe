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
        public decimal RequiredSpending { get; set; }
        public int FreeRequestChange { get; set; }
        public bool PrioritySupport { get; set; }
        public double CommissionDiscount { get; set; }
        public virtual ICollection<Account> Accounts { get; set; }
        public virtual ICollection<Voucher> Vouchers { get; set; }
    }
}
