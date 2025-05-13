using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class ThemeColor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ColorCode { get; set; }

        public virtual ICollection<DecorServiceThemeColor> DecorServiceThemeColors { get; set; } = new List<DecorServiceThemeColor>();
        public virtual ICollection<BookingThemeColor> BookingThemeColors { get; set; } = new List<BookingThemeColor>();
    }
}
