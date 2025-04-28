using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelRequest.Review;
using BusinessLogicLayer.ModelResponse.Pagination;
using BusinessLogicLayer.ModelResponse.Review;
using BusinessLogicLayer.ModelRequest.Pagination;

namespace BusinessLogicLayer.Interfaces
{
    public interface IReviewService
    {
        Task<BaseResponse> GetReviewList();
        Task<BaseResponse> GetReviewById(int id);
        Task<BaseResponse<PageResult<ReviewResponse>>> GetReviewByAccount(int accountId, ReviewFilterRequest request);
        Task<BaseResponse> GetReviewByServiceId(int serviceId);
        Task<BaseResponse> GetReviewByProductId(int productId);
        Task<BaseResponse> CreateOrderReview(int accountId, ReviewOrderRequest request);
        Task<BaseResponse> CreateBookingReview(int accountId, ReviewBookingRequest request);
        Task<BaseResponse> UpdateOrderReview(int id, int productId, int orderId, UpdateOrderReviewRequest request);
        Task<BaseResponse> UpdateBookingReview(int id, int serviceId, int bookingId, UpdateBookingReviewRequest request);
        Task<BaseResponse> DeleteReview(int id);
    }
}
