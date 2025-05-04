using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Order;
using BusinessLogicLayer.ModelResponse.Pagination;
using BusinessLogicLayer.Utilities.Zoom;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Repository.UnitOfWork;
using static DataAccessObject.Models.Booking;

namespace BusinessLogicLayer.Services
{
    public class ZoomService : IZoomService
    {
        private readonly HttpClient _httpClient;
        private readonly ZoomSettings _zoomSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ZoomService(HttpClient httpClient, IOptions<ZoomSettings> options, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _httpClient = httpClient;
            _zoomSettings = options.Value;
            _unitOfWork = unitOfWork;
            _mapper = mapper;

            _httpClient.BaseAddress = new Uri(_zoomSettings.ApiBaseUrl);
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var credentials = $"{_zoomSettings.ClientId}:{_zoomSettings.ClientSecret}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            var request = new HttpRequestMessage(HttpMethod.Post, _zoomSettings.TokenEndPoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "account_credentials" },
                { "account_id", _zoomSettings.AccountId }
            });

            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Unable to obtain Zoom access token: {errorContent}");
            }

            var responseData = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<ZoomTokenResponse>(responseData);

            return tokenResponse?.AccessToken;
        }

        public async Task<ZoomMeetingResponse> CreateMeetingAsync(ZoomMeetingRequest request)
        {
            var accessToken = await GetAccessTokenAsync(); // Lấy access token từ Zoom

            var meetingRequest = new
            {
                topic = request.Topic,
                type = 2, // Scheduled meeting
                start_time = request.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = request.Duration,
                timezone = request.TimeZone,
                settings = new
                {
                    join_before_host = true,
                    waiting_room = true
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(meetingRequest), Encoding.UTF8, "application/json");

            // Gắn Authorization đúng cách
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsync("users/me/meetings", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ZoomMeetingResponse>(result);
            }

            var errorContent = await response.Content.ReadAsStringAsync();

            try
            {
                var errorObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorContent);
                var errorMessage = errorObj.ContainsKey("message") ? errorObj["message"]?.ToString() : "No message";
                var errorCode = errorObj.ContainsKey("code") ? errorObj["code"]?.ToString() : "No code";

                throw new Exception($"Failed to create Zoom meeting.\nStatus: {response.StatusCode}\nError code: {errorCode}\nMessage: {errorMessage}");
            }
            catch (Exception)
            {
                throw new Exception($"Failed to create Zoom meeting.\nStatus: {response.StatusCode}\nRaw Error: {errorContent}");
            }
        }

        public async Task<BaseResponse> CreateMeetingRequestAsync(string bookingCode, CreateMeetingRequest request)
        {
            var response = new BaseResponse();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                                                .Include(b => b.DecorService)
                                                .Where(b => b.BookingCode == bookingCode)
                                                .FirstOrDefaultAsync();

                if (booking == null)
                {
                    response.Message = "Booking not found!";
                    return response;
                }

                var validStatuses = new[] {
                    BookingStatus.Planning,
                    BookingStatus.Quoting,
                    BookingStatus.Contracting,
                    BookingStatus.Confirm,
                    BookingStatus.DepositPaid,
                    BookingStatus.Preparing,
                    BookingStatus.InTransit
                };

                if (!validStatuses.Contains(booking.Status))
                {
                    response.Message = "Invalid status phase!";
                    return response;
                }

                var decorService = booking.DecorService.Style;

                var zoomMeeting = new ZoomMeeting
                {
                    Topic = "{decorService} Booking Request Meeting",
                    StartTime = request.StartTime,
                    CreateAt = DateTime.Now,
                    Status = ZoomMeeting.MeetingStatus.Requested,
                    BookingId = booking.Id,
                    AccountId = request.CustomerId,
                };

                await _unitOfWork.ZoomRepository.InsertAsync(zoomMeeting);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Meeting request created successfully.";
                response.Data = zoomMeeting;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error creating meeting request!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> AcceptMeetingRequestAsync(string bookingCode)
        {
            var response = new BaseResponse();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.Queryable()
                                    .Include(z => z.Booking)
                                        .ThenInclude(b => b.DecorService)
                                    .Where(z => z.Booking.BookingCode == bookingCode && z.Status == ZoomMeeting.MeetingStatus.Requested)
                                    .FirstOrDefaultAsync();

                if (meeting == null)
                {
                    response.Message = "No matching meeting request found!";
                    return response;
                }

                const int defaultDuration = 1440;

                var zoomRequest = new ZoomMeetingRequest
                {
                    Topic = meeting.Topic,
                    TimeZone = "Asia/Ho_Chi_Minh",
                    StartTime = meeting.StartTime,
                    Duration = defaultDuration,
                };

                var zoomResponse = await CreateMeetingAsync(zoomRequest);

                meeting.ZoomUrl = zoomResponse.JoinUrl;
                meeting.Duration = defaultDuration;
                meeting.Status = ZoomMeeting.MeetingStatus.Scheduled;
                meeting.ResponseAt = DateTime.Now;

                _unitOfWork.ZoomRepository.Update(meeting);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Meeting request accepted successfully.";
                response.Data = zoomResponse;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error accepting meeting request!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> RejectMeetingRequestAsync(string bookingCode)
        {
            var response = new BaseResponse();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.Queryable()
                            .Include(z => z.Booking)
                            .Where(z => z.Booking.BookingCode == bookingCode && z.Status == ZoomMeeting.MeetingStatus.Requested)
                            .FirstOrDefaultAsync();

                if (meeting == null)
                {
                    response.Message = "No matching meeting request found!";
                    return response;
                }

                meeting.Status = ZoomMeeting.MeetingStatus.Rejected;
                meeting.ResponseAt = DateTime.Now;

                _unitOfWork.ZoomRepository.Update(meeting);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Meeting request rejected successfully.";
                response.Data = meeting;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error rejecting meeting request!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<PageResult<MeetingListResponse>>> GetMeetingByBookingAsync(int accountId, ZoomFilterRequest request)
        {
            var response = new BaseResponse<PageResult<MeetingListResponse>>();
            try
            {

                // Filter
                Expression<Func<ZoomMeeting, bool>> filter = meeting =>
                    meeting.AccountId == accountId &&
                    meeting.Booking.BookingCode == request.BookingCode &&
                    (!request.Status.HasValue || meeting.Status == request.Status);

                // Sort
                Expression<Func<ZoomMeeting, object>> orderByExpression = request.SortBy?.ToLower() switch
                {
                    "status" => meeting => meeting.Status,
                    _ => meeting => meeting.CreateAt
                };

                // Include entities
                Expression<Func<ZoomMeeting, object>>[] includeProperties =
                {
                    z => z.Booking
                };

                // Get paginated data and filter
                (IEnumerable<ZoomMeeting> meetings, int totalCount) = await _unitOfWork.ZoomRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    includeProperties
                );

                var meeting = _mapper.Map<List<MeetingListResponse>>(meetings);

                var pageResult = new PageResult<MeetingListResponse>
                {
                    Data = meeting,
                    TotalCount = totalCount
                };

                response.Success = true;
                response.Message = "Retrieving meeting list successfully.";
                response.Data = pageResult;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving meeting list!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<MeetingDetailResponse>> GetMeetingById(int meetingId)
        {
            var response = new BaseResponse<MeetingDetailResponse>();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.GetByIdAsync(meetingId);

                if (meeting == null)
                {
                    response.Message = "Meeting not found!";
                    return response;
                }

                response.Success = true;
                response.Message = "Error fetching meeting detail";
                response.Data = _mapper.Map<MeetingDetailResponse>(meeting);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching meeting detail!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}
