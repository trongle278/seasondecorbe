using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessObject.Models
{
    public class FavoriteService
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Reference to the user who favorited the service
        public int AccountId { get; set; }
        [ForeignKey("AccountId")]
        public Account Account { get; set; }

        // Reference to the decor service being favorited
        public int DecorServiceId { get; set; }
        [ForeignKey("DecorServiceId")]
        public DecorService DecorService { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 