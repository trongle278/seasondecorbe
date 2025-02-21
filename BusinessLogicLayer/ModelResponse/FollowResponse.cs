using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class FollowResponse
    {
        public int Id { get; set; }
        public int FollowingId { get; set; }
        public string FollowingName { get; set; }
        public string FollowingAvatar { get; set; }
        public string CreatedAt { get; set; }
    }

    public class FollowerResponse
    {
        public int Id { get; set; }
        // Thông tin của người theo dõi (follower)
        public int FollowerId { get; set; }
        public string FollowerName { get; set; }
        public string FollowerAvatar { get; set; }
        public string FollowedAt { get; set; }
    }

    public class FollowingResponse
    {
        public int Id { get; set; }
        // Thông tin của người được theo dõi (following)
        public int FollowingId { get; set; }
        public string FollowingName { get; set; }
        public string FollowingAvatar { get; set; }
        public string FollowedAt { get; set; }
    }
}
