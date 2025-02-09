using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Enums;

namespace DataAccessObject.Models
{
    public class Decorator
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Nickname { get; set; }
        public string? Bio { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? SeasonalSpecialties { get; set; }
        public string? PortfolioURL { get; set; }

        [Required]
        public DecoratorApplicationStatus Status { get; set; } = DecoratorApplicationStatus.Pending;

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public virtual ICollection<DecorService> DecorServices { get; set; }
    }
}
