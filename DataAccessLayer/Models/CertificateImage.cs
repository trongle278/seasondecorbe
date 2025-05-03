using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class CertificateImage
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public string ImageUrl { get; set; }
    }
}
