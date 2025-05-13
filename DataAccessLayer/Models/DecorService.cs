using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class DecorService
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Style { get; set; }
        public double? BasePrice { get; set; }
        public string Description { get; set; }
        public string Sublocation { get; set; }
        public DateTime CreateAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int DecorCategoryId { get; set; }
        public DecorCategory DecorCategory { get; set; }
        public DateTime StartDate { get; set; }
        public enum DecorServiceStatus
        {
            Available,
            NotAvailable,
            Incoming
        }

        public DecorServiceStatus Status { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<DecorImage>? DecorImages { get; set; }
        // Quan hệ Many-to-Many với Season
        public virtual ICollection<DecorServiceSeason> DecorServiceSeasons { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<FavoriteService> FavoriteServices { get; set; } = new List<FavoriteService>();
        public virtual ICollection<DecorServiceOffering> DecorServiceOfferings { get; set; } = new List<DecorServiceOffering>();


        //1 dịch vụ add được nhiều màu
        public virtual ICollection<DecorServiceThemeColor> DecorServiceThemeColors { get; set; } = new List<DecorServiceThemeColor>();
        //1 dịch vụ add được nhiều style
        public virtual ICollection<DecorServiceStyle> DecorServiceStyles { get; set; } = new List<DecorServiceStyle>();
    }
}
