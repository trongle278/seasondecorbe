using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class ContactResponse
    {
        public int ContactId { get; set; }
        public string ContactName { get; set; }
        public string? Avatar { get; set; }
        public string Message { get; set; }
        public string? LastMessageTime { get; set; }
    }
}
