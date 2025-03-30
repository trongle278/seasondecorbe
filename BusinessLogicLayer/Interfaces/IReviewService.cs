using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelRequest.Review;

namespace BusinessLogicLayer.Interfaces
{
    public interface IReviewService
    {
        Task<BaseResponse> GetReviewList();
        Task<BaseResponse> GetReviewById(int id);
        Task<BaseResponse> GetReviewByServiceId(int serviceId);
        Task<BaseResponse> GetReviewByProductId(int productId);
        Task<BaseResponse> CreateOrderReview(ReviewOrderRequest request);
        Task<BaseResponse> CreateBookingReview(ReviewBookingRequest request);
        Task<BaseResponse> UpdateOrderReview(UpdateOrderReviewRequest request);
        Task<BaseResponse> UpdateBookingReview(UpdateBookingReviewRequest request);
        Task<BaseResponse> DeleteReview(int id);
    }
}
