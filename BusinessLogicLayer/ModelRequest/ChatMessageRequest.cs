using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class Base64FileDto
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string Base64Content { get; set; }
    }

    public class ChatMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Message { get; set; }
        public List<Base64FileDto> Files { get; set; } = new List<Base64FileDto>();
    }
}
