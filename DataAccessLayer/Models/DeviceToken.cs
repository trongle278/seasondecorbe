using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class DeviceToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public string Token { get; set; }

        public string Platform { get; set; } // Ví dụ: "iOS", "Android"

        public DateTime UpdatedAt { get; set; }

    }
}
