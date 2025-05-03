using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class DecorationStyle
    {
        public int Id { get; set; }
        public string Name { get; set; } // Modern, Scandinavian, etc.
        public ICollection<Account> Accounts { get; set; }
    }

}
