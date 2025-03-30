using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Review;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Review;
using Nest;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;

        public ReviewService(IUnitOfWork unitOfWork, IMapper mapper, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<BaseResponse> GetReviewList()
        {
            var response = new BaseResponse();
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetAllAsync();

                response.Success = true;
                response.Message = "Review list retrieved successfully";
                response.Data = _mapper.Map<List<ReviewResponse>>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving review list";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> GetReviewByServiceId(int serviceId)
        {
            var response = new BaseResponse();
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(serviceId);

                if (review == null)
                {
                    response.Success = false;
                    response.Message = "Invalid order";
                    return response;
                }

                response.Success = true;
                response.Message = "Review retrieved successfully";
                response.Data = _mapper.Map<List<ReviewResponse>>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving review";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public Task<BaseResponse> GetReviewByProductId(int productId)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse> GetReviewById(int id)
        {
            var response = new BaseResponse();
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(id);

                if (review == null)
                {
                    response.Success = false;
                    response.Message = "Invalid order";
                    return response;
                }

                response.Success = true;
                response.Message = "Review retrieved successfully";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving review";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public Task<BaseResponse> CreateOrderReview(ReviewOrderRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse> CreateBookingReview(ReviewBookingRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse> UpdateOrderReview(UpdateOrderReviewRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse> UpdateBookingReview(UpdateBookingReviewRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse> DeleteReview(int id)
        {
            throw new NotImplementedException();
        }
    }
}
