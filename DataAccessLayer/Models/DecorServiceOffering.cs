using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class DecorServiceOffering
    {
        public int DecorServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public int OfferingId { get; set; }
        public Offering Offering { get; set; }
    }
}
