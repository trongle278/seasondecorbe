using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class WalletTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int WalletId { get; set; }

        public virtual Wallet Wallet { get; set; }

        public int PaymentTransactionId { get; set; }

        public virtual PaymentTransaction PaymentTransaction { get; set; }
    }
}
