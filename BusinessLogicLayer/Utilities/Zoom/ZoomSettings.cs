using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Utilities.Zoom
{
    public class ZoomSettings
    {
        public string AccountId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TokenEndPoint { get; set; }
        public string ApiBaseUrl { get; set; }
    }
}
