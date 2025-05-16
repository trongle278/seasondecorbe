using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class AccountCategoryPreference
    {
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int DecorCategoryId { get; set; }
        public DecorCategory DecorCategory { get; set; }
    }
}
