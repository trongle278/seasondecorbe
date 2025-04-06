using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelResponse.Review
{
    public class ReviewResponse
    {
        public int id { get; set; }
        public int Rate { get; set; }
        public string Comment { get; set; }
        public DateTime CreateAt { get; set; }
        public List<string> Images { get; set; }
    }

    public class ProductReviewResponse
    {
        public int id { get; set; }
        public int ProductId { get; set; }
        public int Rate { get; set; }
        public string Comment { get; set; }
        public DateTime CreateAt { get; set; }
        public List<string> Images { get; set; }
    }

    public class ServiceReviewResponse
    {
        public int id { get; set; }
        public int ServiceId { get; set; }
        public int Rate { get; set; }
        public string Comment { get; set; }
        public DateTime CreateAt { get; set; }
        public List<string> Images { get; set; }
    }

    public class UpdateProductReviewResponse
    {
        public int id { get; set; }
        public int ProductId { get; set; }
        public int Rate { get; set; }
        public string Comment { get; set; }
        public DateTime? UpdateAt { get; set; }
        public bool IsUpdated { get; set; }
        public List<string> Images { get; set; }
    }

    public class UpdateServiceReviewResponse
    {
        public int id { get; set; }
        public int ServiceId { get; set; }
        public int Rate { get; set; }
        public string Comment { get; set; }
        public DateTime? UpdateAt { get; set; }
        public bool IsUpdated { get; set; }
        public List<string> Images { get; set; }
    }
}
