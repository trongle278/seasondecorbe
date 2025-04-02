using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest.Review;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Review;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;
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
                var review = await _unitOfWork.ReviewRepository.Queryable()
                                            .Where(r => r.ServiceId == serviceId)
                                            .Include(r => r.ReviewImages)
                                            .ToListAsync();

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

        public async Task<BaseResponse> GetReviewByProductId(int productId)
        {

            var response = new BaseResponse();
            try
            {
                var review = await _unitOfWork.ReviewRepository.Queryable()
                                            .Where(r => r.ProductId == productId)
                                            .Include(r => r.ReviewImages)
                                            .ToListAsync();

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

        public async Task<BaseResponse> GetReviewById(int id)
        {
            var response = new BaseResponse();
            try
            {
                var review = await _unitOfWork.ReviewRepository.GetByIdAsync(id);

                if (review == null)
                {
                    response.Success = false;
                    response.Message = "Invalid review";
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

        public async Task<BaseResponse> CreateOrderReview(ReviewOrderRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var order = await _unitOfWork.OrderRepository.Queryable()
                                            .Where(o => o.AccountId == request.AccountId && o.Id == request.OrderId && o.Status == Order.OrderStatus.Completed)
                                            .Include(o => o.OrderDetails)
                                            .FirstOrDefaultAsync(o => o.OrderDetails.Any(od => od.ProductId == request.ProductId));

                if (order == null)
                {
                    response.Success = false;
                    response.Message = "Invalid order";
                    return response;
                }

                var existingReview = await _unitOfWork.ReviewRepository.Queryable()
                                                    .Where(r => r.AccountId == request.AccountId && r.ProductId == request.ProductId && r.OrderId == request.OrderId)
                                                    .FirstOrDefaultAsync();

                if (existingReview != null)
                {
                    response.Success = false;
                    response.Message = "Product reviewed";
                    return response;
                }

                var review = new Review
                {
                    AccountId = request.AccountId,
                    OrderId = request.OrderId,
                    ProductId = request.ProductId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreateAt = DateTime.UtcNow.ToLocalTime(),
                    IsUpdated = false,
                    ReviewImages = new List<ReviewImage>()
                };

                // Upload images
                if (request.Images != null && request.Images.Any())
                {
                    foreach (var imageFile in request.Images)
                    {
                        using var stream = imageFile.OpenReadStream();
                        var imageUrl = await _cloudinaryService.UploadFileAsync(
                            stream,
                            imageFile.FileName,
                            imageFile.ContentType
                            );
                        review.ReviewImages.Add(new ReviewImage { ImageUrl = imageUrl });
                    }
                }

                await _unitOfWork.ReviewRepository.InsertAsync(review);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Review product successfully";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error reviewing product";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CreateBookingReview(ReviewBookingRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                                            .Where(b => b.AccountId == request.AccountId && b.Id == request.BookingId && b.Status == Booking.BookingStatus.Completed)
                                            .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Success = false;
                    response.Message = "Invalid booking";
                    return response;
                }

                var existingReview = await _unitOfWork.ReviewRepository.Queryable()
                                                    .Where(r => r.AccountId == request.AccountId && r.ServiceId == request.ServiceId && r.BookingId == request.BookingId)
                                                    .FirstOrDefaultAsync();

                if (existingReview != null)
                {
                    response.Success = false;
                    response.Message = "Service reviewed";
                    return response;
                }

                var review = new Review
                {
                    AccountId = request.AccountId,
                    BookingId = request.BookingId,
                    ServiceId = request.ServiceId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreateAt = DateTime.UtcNow.ToLocalTime(),
                    IsUpdated = false,
                    ReviewImages = new List<ReviewImage>()
                };

                // Upload images
                if (request.Images != null && request.Images.Any())
                {
                    foreach (var imageFile in request.Images)
                    {
                        using var stream = imageFile.OpenReadStream();
                        var imageUrl = await _cloudinaryService.UploadFileAsync(
                            stream,
                            imageFile.FileName,
                            imageFile.ContentType
                            );
                        review.ReviewImages.Add(new ReviewImage { ImageUrl = imageUrl });
                    }
                }

                await _unitOfWork.ReviewRepository.InsertAsync(review);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Review service successfully";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error reviewing service";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateOrderReview(int id, int productId, int orderId, UpdateOrderReviewRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var review = await _unitOfWork.ReviewRepository.Queryable()
                                            .Where(r => r.Id == id)
                                            .Include(r => r.ReviewImages)
                                            .FirstOrDefaultAsync();

                if (review == null)
                {
                    response.Success = false;
                    response.Message = "Invalid review";
                    return response;
                }

                if (review.ProductId != productId)
                {
                    response.Success = false;
                    response.Message = "Invalid product";
                    return response;
                }

                if (review.OrderId != orderId)
                {
                    response.Success = false;
                    response.Message = "Invalid order";
                    return response;
                }

                if ((DateTime.UtcNow.ToLocalTime() - review.CreateAt).TotalDays > 3)
                {
                    response.Success = false;
                    response.Message = "Expired";
                    return response;
                }

                review.Rating = request.Rating;
                review.Comment = request.Comment;
                review.UpdateAt = DateTime.UtcNow.ToLocalTime();
                review.IsUpdated = true;

                // Upload images
                if (request.Images != null && request.Images.Any())
                {
                    if (review.ReviewImages.Any())
                    {
                        foreach (var imageFile in request.Images)
                        {
                            review.ReviewImages.Clear();

                            using var stream = imageFile.OpenReadStream();
                            var imageUrl = await _cloudinaryService.UploadFileAsync(
                                stream,
                                imageFile.FileName,
                                imageFile.ContentType
                                );
                            review.ReviewImages.Add(new ReviewImage { ImageUrl = imageUrl });
                        }
                    }                    
                }

                _unitOfWork.ReviewRepository.Update(review);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Review updated successfully";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating review";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateBookingReview(int id, int serviceId, int bookingId, UpdateBookingReviewRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var review = await _unitOfWork.ReviewRepository.Queryable()
                                            .Where(r => r.Id == id)
                                            .Include(r => r.ReviewImages)
                                            .FirstOrDefaultAsync();

                if (review == null)
                {
                    response.Success = false;
                    response.Message = "Invalid review";
                    return response;
                }

                if (review.ServiceId != serviceId)
                {
                    response.Success = false;
                    response.Message = "Invalid service";
                    return response;
                }

                if (review.BookingId != bookingId)
                {
                    response.Success = false;
                    response.Message = "Invalid booking";
                    return response;
                }

                if ((DateTime.UtcNow.ToLocalTime() - review.CreateAt).TotalDays > 3)
                {
                    response.Success = false;
                    response.Message = "Expired";
                    return response;
                }

                review.Rating = request.Rating;
                review.Comment = request.Comment;
                review.UpdateAt = DateTime.UtcNow.ToLocalTime();
                review.IsUpdated = true;

                // Upload images
                if (request.Images != null && request.Images.Any())
                {
                    if (review.ReviewImages.Any())
                    {
                        foreach (var imageFile in request.Images)
                        {
                            review.ReviewImages.Clear();

                            using var stream = imageFile.OpenReadStream();
                            var imageUrl = await _cloudinaryService.UploadFileAsync(
                                stream,
                                imageFile.FileName,
                                imageFile.ContentType
                                );
                            review.ReviewImages.Add(new ReviewImage { ImageUrl = imageUrl });
                        }
                    }
                }

                _unitOfWork.ReviewRepository.Update(review);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Review updated successfully";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating review";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> DeleteReview(int id)
        {
            var response = new BaseResponse();
            try
            {
                var review = await _unitOfWork.ReviewRepository.Queryable()
                                            .Where(r => r.Id == id)
                                            .Include(r => r.ReviewImages)
                                            .FirstOrDefaultAsync();

                if (review == null)
                {
                    response.Success = false;
                    response.Message = "Invalid review";
                    return response;
                }

                if ((DateTime.UtcNow.ToLocalTime() - review.CreateAt).TotalDays > 3)
                {
                    response.Success = false;
                    response.Message = "Expired";
                    return response;
                }

                // Delete Images
                if (review.ReviewImages != null && review.ReviewImages.Any())
                {
                    _unitOfWork.ReviewImageRepository.RemoveRange(review.ReviewImages);
                }

                _unitOfWork.ReviewRepository.Delete(review);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Review Deleted successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting review";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
