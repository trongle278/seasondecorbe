using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class TrackingImage
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Tracking")]
        public int TrackingId { get; set; }
        public virtual Tracking Tracking { get; set; }

        public string ImageUrl { get; set; }
    }

}
