using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Address
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } 

        [Required]
        public string Phone{ get; set; }

        public enum AddressType
        {
            Home,
            Office
        }

        public AddressType Type { get; set; }

        // Có thể đánh dấu địa chỉ mặc định
        public bool IsDefault { get; set; } = false;

        public string? Province { get; set; }    // Tỉnh / Thành phố
        public string? District { get; set; }    // Quận / Huyện
        public string? Ward { get; set; }        // Phường / Xã
        public string? Street { get; set; }      // Tên đường
        public string? Detail { get; set; }      // Thông tin chi tiết (số nhà, hẻm,...)
        public bool IsDelete { get; set; }

        public int AccountId { get; set; }
        public virtual Account Account { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}
