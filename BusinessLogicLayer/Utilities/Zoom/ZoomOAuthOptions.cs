using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Utilities.Zoom
{
    public class ZoomOAuthOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string TokenEndpoint { get; set; }
        public string AuthEndpoint { get; set; }
        public string ApiBaseUrl { get; set; }
    }
}
