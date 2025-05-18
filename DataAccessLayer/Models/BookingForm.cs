using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class BookingForm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? SpaceStyle { get; set; }
        public double? RoomSize { get; set; }
        public string? Style { get; set; }
        public string? ThemeColor { get; set; }
        public string? PrimaryUser { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public Booking Booking { get; set; }

        public virtual ICollection<FormImage>? FormImages { get; set; }
        public virtual ICollection<ScopeOfWorkForm>? ScopeOfWorkForms { get; set; }
    }
}
