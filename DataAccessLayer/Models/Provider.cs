using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Provider
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Name { get; set; }
        [NotMapped]
        public int TotalRate { get; set; }
        [NotMapped]
        public int TotalProduct { get; set; }
        [NotMapped]
        public int Follower { get; set; }
        [NotMapped]
        public int Following { get; set; }
        public string? Bio { get; set; }
        public string? Address { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsProvider { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }

        //public virtual ICollection<DecorService> DecorServices { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}
