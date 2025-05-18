using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class ScopeOfWorkForm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public int BookingFormId { get; set; }
        public BookingForm BookingForm { get; set; }

        public int ScopeOfWorkId { get; set; }
        public ScopeOfWork ScopeOfWork { get; set; }
    }
}
