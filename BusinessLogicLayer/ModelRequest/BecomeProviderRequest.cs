using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class BecomeProviderRequest
    {
        [Required]
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}
