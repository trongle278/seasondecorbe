using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class BecomeDecoratorRequest
    {
        [Required]
        public string Nickname { get; set; }
        public string? Bio { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? SeasonalSpecialties { get; set; }
        public string? PortfolioURL { get; set; }
    }
}
