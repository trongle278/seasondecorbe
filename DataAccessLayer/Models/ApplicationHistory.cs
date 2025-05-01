using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class ApplicationHistory
    {
        [Key]
        public int Id { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public string Reason { get; set; }

        public DateTime RejectedAt { get; set; }
        // Lưu thông tin ảnh bị từ chối
    }
}
