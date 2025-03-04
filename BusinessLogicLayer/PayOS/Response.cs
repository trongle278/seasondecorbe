using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.PayOS
{
    public class Response
    {
        public int error { get; set; }
        public string message { get; set; }
        public object? data { get; set; }
        public Response() { }
        public Response(int error, string message, object? data)
        {
            this.error = error;
            this.message = message;
            this.data = data;
        }
    }
}
