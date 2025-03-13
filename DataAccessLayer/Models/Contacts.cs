using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ContactId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();

        // Navigation Properties
        public virtual Account User { get; set; }
        public virtual Account ContactUser { get; set; }
    }
}
