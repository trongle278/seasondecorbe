using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Services
{
    public class TrackingService : ITrackingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly string _clientBaseUrl;

        public TrackingService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IMapper mapper, INotificationService notificationService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
            _notificationService = notificationService;
            _clientBaseUrl = configuration["AppSettings:ClientBaseUrl"];
        }

        public async Task<BaseResponse<List<TrackingResponse>>> GetTrackingByBookingCodeAsync(string bookingCode)
        {
            var response = new BaseResponse<List<TrackingResponse>>();

            try
            {
                // 🔹 Lấy tracking liên quan tới bookingCode
                var trackingHistory = await _unitOfWork.TrackingRepository.Queryable()
                    .Where(t => t.Booking.BookingCode == bookingCode)
                    .Include(t => t.TrackingImages)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();

                if (!trackingHistory.Any())
                {
                    response.Success = true;
                    response.Data = new List<TrackingResponse>();
                    response.Message = "No tracking history found for this booking.";
                    return response;
                }

                // 🔹 Map ra DTO
                var trackingResponses = trackingHistory.Select(t => new TrackingResponse
                {
                    Id = t.Id,
                    Task = t.Task,
                    BookingCode = bookingCode,
                    Note = t.Note,
                    CreatedAt = t.CreatedAt,
                    Images = t.TrackingImages?.Select(img => new TrackingImageResponse
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl
                    }).ToList() ?? new List<TrackingImageResponse>()
                }).ToList();

                response.Success = true;
                response.Data = trackingResponses;
                response.Message = "Tracking history retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to retrieve tracking history.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> AddTrackingAsync(TrackingRequest request, string bookingCode)
        {
            var response = new BaseResponse();

            try
            {
                // 🔹 Lấy Booking theo BookingCode
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Where(b => b.BookingCode == bookingCode)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found.";
                    return response;
                }

                // 🔹 Kiểm tra Booking phải đang ở giai đoạn Progressing
                if (booking.Status != Booking.BookingStatus.Progressing)
                {
                    response.Message = "Tracking can be updated during the Progressing phase.";
                    return response;
                }

                // 🔹 Kiểm tra bắt buộc phải có ít nhất 1 ảnh
                if (request.Images == null || !request.Images.Any())
                {
                    response.Message = "At least one image is required for tracking.";
                    return response;
                }

                // 🔹 Kiểm tra số lượng ảnh tối đa là 5
                if (request.Images.Count() > 5)
                {
                    response.Message = "You can upload a maximum of 5 images.";
                    return response;
                }

                // 🔹 Kiểm tra bắt buộc phải có note
                if (string.IsNullOrWhiteSpace(request.Note))
                {
                    response.Message = "Note is required for tracking.";
                    return response;
                }

                // 🔹 Tạo mới một bản ghi Tracking
                var tracking = new Tracking
                {
                    BookingId = booking.Id,
                    Task = request.Task,
                    Note = request.Note,
                    CreatedAt = DateTime.Now,
                    TrackingImages = new List<TrackingImage>()
                };

                // 🔹 Upload từng ảnh lên Cloudinary
                foreach (var imageFile in request.Images)
                {
                    using var stream = imageFile.OpenReadStream();
                    var imageUrl = await _cloudinaryService.UploadFileAsync(
                        stream,
                        $"tracking_{booking.BookingCode}_{DateTime.Now.Ticks}{Path.GetExtension(imageFile.FileName)}",
                        imageFile.ContentType
                    );

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        tracking.TrackingImages.Add(new TrackingImage { ImageUrl = imageUrl });
                    }
                }

                // 🔹 Lưu tracking vào database
                booking.IsTracked = true;
                await _unitOfWork.TrackingRepository.InsertAsync(tracking);
                await _unitOfWork.CommitAsync();

                string colorbookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{booking.BookingCode}</span>";
                // 🔹 Gửi thông báo cho Customer
                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = booking.AccountId,
                    Title = "Booking Progress Updated",
                    Content = $"Your booking #{colorbookingCode} has new progress update.",
                    Url = $""
                });

                response.Success = true;
                response.Message = "Tracking added successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update tracking.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> UpdateTrackingAsync(UpdateTrackingRequest request, int trackingId)
        {
            var response = new BaseResponse();

            try
            {
                // 🔹 Load Tracking + hình ảnh
                var tracking = await _unitOfWork.TrackingRepository.Queryable()
                    .Include(t => t.TrackingImages)
                    .Where(t => t.Id == trackingId)
                    .FirstOrDefaultAsync();

                if (tracking == null)
                {
                    response.Message = "Tracking not found.";
                    return response;
                }

                // 🔹 Kiểm tra Booking còn trong trạng thái Progressing
                var booking = await _unitOfWork.BookingRepository.Queryable()
                    .Where(b => b.Id == tracking.BookingId)
                    .FirstOrDefaultAsync();

                if (booking == null || booking.Status != Booking.BookingStatus.Progressing)
                {
                    response.Message = "Tracking can only be updated during the Progressing phase.";
                    return response;
                }

                // 🔹 Validate Note
                if (string.IsNullOrWhiteSpace(request.Note))
                {
                    response.Message = "Note is required for tracking.";
                    return response;
                }

                // 🔹 Xử lý ảnh
                if (request.Images != null && request.Images.Any())
                {
                    int currentImageCount = tracking.TrackingImages.Count;
                    int incomingImageCount = request.Images.Count;
                    int totalAfterUpdate = currentImageCount + incomingImageCount;

                    if (totalAfterUpdate > 5)
                    {
                        response.Message = $"You can upload up to 5 images only. Current images: {currentImageCount}.";
                        return response;
                    }

                    // 🔹 Nếu request có ImageIds: update ảnh theo ID
                    if (request.ImageIds != null && request.ImageIds.Any())
                    {
                        for (int i = 0; i < request.ImageIds.Count && i < request.Images.Count; i++)
                        {
                            var imageId = request.ImageIds[i];
                            var imageFile = request.Images[i];

                            var imageToUpdate = tracking.TrackingImages
                                                .Where(x => x.Id == imageId)
                                                .FirstOrDefault();
                            if (imageToUpdate != null)
                            {
                                using var stream = imageFile.OpenReadStream();
                                var imageUrl = await _cloudinaryService.UploadFileAsync(
                                    stream,
                                    $"tracking_{booking.BookingCode}_{DateTime.Now.Ticks}{Path.GetExtension(imageFile.FileName)}",
                                    imageFile.ContentType
                                );

                                if (!string.IsNullOrEmpty(imageUrl))
                                {
                                    imageToUpdate.ImageUrl = imageUrl;
                                }
                            }
                        }
                    }

                    // 🔹 Nếu có ảnh upload mới (không kèm Id): thêm ảnh mới
                    if (request.Images.Count > request.ImageIds?.Count)
                    {
                        for (int i = request.ImageIds?.Count ?? 0; i < request.Images.Count; i++)
                        {
                            var newImageFile = request.Images[i];

                            using var stream = newImageFile.OpenReadStream();
                            var imageUrl = await _cloudinaryService.UploadFileAsync(
                                stream,
                                $"tracking_{booking.BookingCode}_{DateTime.Now.Ticks}{Path.GetExtension(newImageFile.FileName)}",
                                newImageFile.ContentType
                            );

                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                tracking.TrackingImages.Add(new TrackingImage { ImageUrl = imageUrl });
                            }
                        }
                    }
                }

                // 🔹 Update Task và Note
                tracking.Task = request.Task;
                tracking.Note = request.Note;
                // tracking.CreatedAt giữ nguyên, KHÔNG update CreatedAt lúc update nội dung

                _unitOfWork.TrackingRepository.Update(tracking);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Tracking updated successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update tracking.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> RemoveTrackingAsync(int trackingId)
        {
            var response = new BaseResponse();

            try
            {
                // 🔹 Lấy tracking
                var tracking = await _unitOfWork.TrackingRepository.Queryable()
                    .Include(t => t.TrackingImages)
                    .Where(t => t.Id == trackingId)
                    .FirstOrDefaultAsync();

                if (tracking == null)
                {
                    response.Message = "Tracking not found.";
                    return response;
                }

                // 🔹 Xóa hết ảnh liên quan
                if (tracking.TrackingImages != null && tracking.TrackingImages.Any())
                {
                    _unitOfWork.TrackingImageRepository.RemoveRange(tracking.TrackingImages);
                }

                // 🔹 Xóa tracking
                _unitOfWork.TrackingRepository.RemoveEntity(tracking);

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Tracking removed successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to remove tracking.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
