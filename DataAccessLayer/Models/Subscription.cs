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
        public string Description { get; set; }
        public double Price { get; set; }
        public int Duration { get; set; }
        //public enum SubscriptionStatus
        //{
        //    Subcribed,
        //    Unsubcribed
        //}
        //public SubscriptionStatus Status { get; set; }

        public virtual ICollection<Account> Accounts { get; set; }
        public virtual ICollection<Provider> Providers { get; set; }
    }
}
