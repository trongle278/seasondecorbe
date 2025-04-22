using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class TrackingResponse
    {
        public string BookingCode { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
    }

}
