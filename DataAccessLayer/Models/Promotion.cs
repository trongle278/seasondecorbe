using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Promotion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string PromotionName { get; set; }
        public int Promote { get; set; }
        public string Image { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public virtual ICollection<ServicePromote> ServicePromotes { get; set; }
    }
}
