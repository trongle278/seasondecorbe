using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class ChatMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Message { get; set; }
    }
}
