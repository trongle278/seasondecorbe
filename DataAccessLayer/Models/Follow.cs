using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Follow
    {
        [Key]
        public int Id { get; set; }

        public int FollowerId { get; set; }
        [ForeignKey("FollowerId")]
        public virtual Account Follower { get; set; }

        public int FollowingId { get; set; }
        [ForeignKey("FollowingId")]
        public virtual Account Following { get; set; }

        public bool IsNotify { get; set; } = true;  // tuỳ chọn, nếu cần cài đặt thông báo
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
