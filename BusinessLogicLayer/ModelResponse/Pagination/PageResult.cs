using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse.Review;

namespace BusinessLogicLayer.ModelResponse.Pagination
{
    public class PageResult<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int TotalCount { get; set; }
    }

    public class ReviewPageResult : PageResult<ReviewResponse>
    {
        public double AverageRate { get; set; }
        public Dictionary<int, int> RateCount { get; set; }
    }
}
