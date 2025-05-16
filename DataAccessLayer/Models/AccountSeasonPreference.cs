using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class AccountSeasonPreference
    {
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int SeasonId { get; set; }
        public Season Season { get; set; }
    }

}
