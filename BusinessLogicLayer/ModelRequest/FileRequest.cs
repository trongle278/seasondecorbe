using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class FileRequest
    {
        public string FileName { get; set; }       // Tên file
        public string Base64Content { get; set; }  // Nội dung file dưới dạng base64
        public string ContentType { get; set; }    // Loại file (ví dụ: image/png, application/pdf)
    }
}
