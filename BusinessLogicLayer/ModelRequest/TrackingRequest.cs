using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelRequest
{
    public class TrackingRequest
    {
        public string Task { get; set; }
        public string? Note { get; set; }
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }

    public class UpdateTrackingRequest
    {
        public string Task { get; set; }  // New task description
        public string Note { get; set; }  // New note for tracking
        //thêm id ảnh để sửa
        public List<int> ImageIds { get; set; } = new List<int>();
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }
}
