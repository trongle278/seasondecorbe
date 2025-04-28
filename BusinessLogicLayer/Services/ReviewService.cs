using System;
using System.Collections.Generic;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Nest;
using Repository.UnitOfWork;
using BusinessLogicLayer.ModelResponse.Product;
using BusinessLogicLayer.ModelResponse.Pagination;
using BusinessLogicLayer.ModelRequest.Pagination;

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

        public async Task<BaseResponse<PageResult<ReviewResponse>>> GetReviewByAccount(int accountId, ReviewFilterRequest request)
        {
            var response = new BaseResponse<PageResult<ReviewResponse>>();
            try
            {
                var account = await _unitOfWork.AccountRepository
                                                .Query(a => a.Id == accountId)
                                                .FirstOrDefaultAsync();

                if (account == null)
                {
                    response.Message = "Account not found!";
                    return response;
                }

                // Filter
                Expression<Func<Review, bool>> filter = review =>
                    review.AccountId == accountId &&
                    (!request.Rate.HasValue || review.Rate == request.Rate);

                // Sort
                Expression<Func<Review, object>> orderByExpression = request.SortBy switch
                {
                    "CreateAt" => review => review.CreateAt,
                    "UpdateAt" => review => review.UpdateAt ?? review.CreateAt,
                    _ => review => review.Id
                };

                // Include Images
                Expression<Func<Review, object>>[] includeProperties = { r => r.ReviewImages };

                // Get paginated data and filter
                (IEnumerable<Review> reviews, int totalCount) = await _unitOfWork.ReviewRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    includeProperties
                );

                var reviewResponses = _mapper.Map<List<ReviewResponse>>(reviews);

                var pageResult = new PageResult<ReviewResponse>
                {
                    Data = reviewResponses,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Review list retrieved successfully";
                response.Data = pageResult;
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
                    response.Message = "Review not found!";
                    return response;
                }

                response.Success = true;
                response.Message = "Review retrieved successfully.";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving review!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CreateOrderReview(int accountId, ReviewOrderRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var order = await _unitOfWork.OrderRepository.Queryable()
                                            .Where(o => o.AccountId == accountId && o.Id == request.OrderId && o.Status == Order.OrderStatus.Paid)
                                            .Include(o => o.OrderDetails)
                                            .FirstOrDefaultAsync(o => o.OrderDetails.Any(od => od.ProductId == request.ProductId));

                if (order == null)
                {
                    response.Message = "Product has to be ordered before review!";
                    return response;
                }

                var existingReview = await _unitOfWork.ReviewRepository.Queryable()
                                                    .Where(r => r.AccountId == accountId && r.ProductId == request.ProductId && r.OrderId == request.OrderId)
                                                    .FirstOrDefaultAsync();

                if (existingReview != null)
                {
                    response.Message = "Product has been reviewed";
                    return response;
                }

                var review = new Review
                {
                    AccountId = accountId,
                    OrderId = request.OrderId,
                    ProductId = request.ProductId,
                    Rate = request.Rate,
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

        public async Task<BaseResponse> CreateBookingReview(int accountId, ReviewBookingRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                                            .Where(b => b.AccountId == accountId && b.Id == request.BookingId && b.Status == Booking.BookingStatus.Completed)
                                            .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Service has to be booked before review!";
                    return response;
                }

                var serviceId = booking.DecorServiceId;

                var existingReview = await _unitOfWork.ReviewRepository.Queryable()
                                                    .Where(r => r.AccountId == accountId && r.ServiceId == serviceId && r.BookingId == request.BookingId)
                                                    .FirstOrDefaultAsync();

                if (existingReview != null)
                {
                    response.Message = "Booking service has been reviewed!";
                    return response;
                }

                var review = new Review
                {
                    AccountId = accountId,
                    BookingId = request.BookingId,
                    ServiceId = serviceId,
                    Rate = request.Rate,
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

                // Cập nhật trạng thái IsReview của Booking
                booking.IsReviewed = true;
                _unitOfWork.BookingRepository.Update(booking);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Review service successfully.";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error reviewing service!";
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
                    response.Message = "Review not found!";
                    return response;
                }

                if (review.ProductId != productId)
                {
                    response.Message = "Invalid product!";
                    return response;
                }

                if (review.OrderId != orderId)
                {
                    response.Message = "Invalid order!";
                    return response;
                }

                if ((DateTime.UtcNow.ToLocalTime() - review.CreateAt).TotalDays > 3)
                {
                    response.Message = "Review can only be updated within 3 days of creation!";
                    return response;
                }

                review.Rate = request.Rate;
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
                response.Message = "Review updated successfully.";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating review!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateBookingReview(int id, int bookingId, UpdateBookingReviewRequest request)
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
                    response.Message = "Review not found!";
                    return response;
                }

                if (review.BookingId != bookingId)
                {
                    response.Message = "Invalid booking!";
                    return response;
                }

                if ((DateTime.UtcNow.ToLocalTime() - review.CreateAt).TotalDays > 3)
                {
                    response.Message = "Review can only be updated within 3 days of creation!";
                    return response;
                }

                review.Rate = request.Rate;
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
                response.Message = "Review updated successfully.";
                response.Data = _mapper.Map<ReviewResponse>(review);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating review!";
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
                    response.Message = "Review not found!";
                    return response;
                }

                if ((DateTime.UtcNow.ToLocalTime() - review.CreateAt).TotalDays > 3)
                {
                    response.Message = "Review can only be deleted within 3 days of creation!";
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
                response.Message = "Review Deleted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting review!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
