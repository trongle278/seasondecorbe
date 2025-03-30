using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class ReviewImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }

        public int ReviewId { get; set; }
        public Review Review { get; set; }
    }
}
