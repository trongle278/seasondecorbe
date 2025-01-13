using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Follower
    {
        public int FollowerId { get; set; }
        public FollowerActivity FollowerActivity { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }
    }
}
