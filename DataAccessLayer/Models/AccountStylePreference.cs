using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class AccountStylePreference
    {
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int DecorationStyleId { get; set; }
        public DecorationStyle DecorationStyle { get; set; }
    }

}
