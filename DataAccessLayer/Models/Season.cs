using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Season
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string SeasonName { get; set; }

        // Quan hệ Many-to-Many với DecorService
        public virtual ICollection<DecorServiceSeason> DecorServiceSeasons { get; set; }
        public virtual ICollection<ProductSeason> ProductSeasons { get; set; }
    }
}
