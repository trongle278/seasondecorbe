using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    //public class TrackingResponse
    //{
    //    public int Id { get; set; }
    //    public string BookingCode { get; set; }
    //    public string Task { get; set; }
    //    public string Note { get; set; }
    //    public DateTime CreatedAt { get; set; }
    //    public List<string> ImageUrls { get; set; } = new List<string>();
    //}

    public class TrackingImageResponse
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
    }

    public class TrackingResponse
    {
        public int Id { get; set; }
        public string BookingCode { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TrackingImageResponse> Images { get; set; } = new List<TrackingImageResponse>();
    }
}
