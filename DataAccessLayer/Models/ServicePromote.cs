using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class ServicePromote
    {
        public int DecorServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public int PromotionId { get; set; }
        public Promotion Promotion { get; set; }
    }
}
