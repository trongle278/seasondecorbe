using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class DecorService
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Style { get; set; }
        public double BasePrice { get; set; }
        public string Description { get; set; }
        public string Province { get; set; }
        public DateTime CreateAt { get; set; }

        public int DecoratorId { get; set; }
        public Decorator Decorator { get; set; }

        public int DecorCategoryId { get; set; }
        public DecorCategory DecorCategory { get; set; }

        public virtual Booking Booking { get; set; }

        public virtual ICollection<DecorImage> DecorImages { get; set; }
        public virtual ICollection<ServicePromote> ServicePromotes { get; set; }
    }
}
