using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Enums;

namespace DataAccessObject.Models
{
    public class Provider
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsProvider { get; set; }

        [Required]
        public DecoratorApplicationStatus Status { get; set; } = DecoratorApplicationStatus.Pending;

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int SubcriptionId { get; set; }
        public Subscription Subcription { get; set; }

        public virtual ICollection<DecorService> DecorServices { get; set; }
    }
}
