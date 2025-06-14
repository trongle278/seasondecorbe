﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
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
using FirebaseAdmin.Auth.Hash;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Repository.UnitOfWork;
using static DataAccessObject.Models.Booking;

namespace BusinessLogicLayer.Services
{
    public class ZoomService : IZoomService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ZoomSettings _zoomSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ZoomService(IConfiguration configuration, HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IOptions<ZoomSettings> options, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
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
                start_time = request.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                duration = request.Duration,
                timezone = request.TimeZone,
                settings = new
                {
                    join_before_host = true,
                    waiting_room = false
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

        public async Task<bool> CancelMeetingAsync(string meetingId)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();

                var request = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"https://api.zoom.us/v2/meetings/{meetingId}"
                );

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Zoom meeting cancellation failed: {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while cancelling Zoom meeting: {ex.Message}");
                return false;
            }
        }

        public async Task<BaseResponse> CreateMeetingRequestAsync(string bookingCode, int customerId, CreateMeetingRequest request)
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
                    Topic = $"{decorService} Booking Request - Meeting",
                    StartTime = request.StartTime,
                    CreateAt = DateTime.Now,
                    Status = ZoomMeeting.MeetingStatus.Requested,
                    BookingId = booking.Id,
                    AccountId = customerId
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

        public async Task<BaseResponse> AcceptMeetingRequestAsync(string bookingCode, int id)
        {
            var response = new BaseResponse();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.Queryable()
                                    .Include(z => z.Booking)
                                        .ThenInclude(b => b.DecorService)
                                    .Where(z => z.Booking.BookingCode == bookingCode && z.Id == id && z.Status == ZoomMeeting.MeetingStatus.Requested)
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
                meeting.MeetingNumber = zoomResponse.MeetingNumber;
                meeting.Password = zoomResponse.Password;
                meeting.StartTime = zoomResponse.StartTime;
                meeting.StartUrl = zoomResponse.StartUrl;

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

        //public async Task<BaseResponse> AcceptMeetingRequestAsync(string bookingCode, int id)
        //{
        //    var response = new BaseResponse();
        //    try
        //    {
        //        var meeting = await _unitOfWork.ZoomRepository.Queryable()
        //                            .Include(z => z.Booking)
        //                                .ThenInclude(b => b.DecorService)
        //                            .Where(z => z.Booking.BookingCode == bookingCode && z.Id == id && z.Status == ZoomMeeting.MeetingStatus.Requested)
        //                            .FirstOrDefaultAsync();

        //        if (meeting == null)
        //        {
        //            response.Message = "No matching meeting request found!";
        //            return response;
        //        }

        //        const int defaultDuration = 1440;

        //        // Thay đổi: Sử dụng Video SDK Token thay vì tạo meeting với URL
        //        var zoomRequest = new ZoomMeetingRequest
        //        {
        //            Topic = meeting.Topic,
        //            TimeZone = "Asia/Ho_Chi_Minh",
        //            StartTime = meeting.StartTime,
        //            Duration = defaultDuration,
        //        };

        //        // Giả sử bạn đã có phương thức để tạo token cho Video SDK
        //        var token = await GenerateVideoSdkToken(meeting.Topic);

        //        // Cập nhật các thông tin liên quan đến cuộc họp
        //        meeting.Status = ZoomMeeting.MeetingStatus.Scheduled;
        //        meeting.Duration = defaultDuration;
        //        meeting.StartTime = zoomRequest.StartTime;
        //        meeting.Token = token; // Lưu token Video SDK để dùng sau

        //        _unitOfWork.ZoomRepository.Update(meeting);
        //        await _unitOfWork.CommitAsync();

        //        response.Success = true;
        //        response.Message = "Meeting request accepted successfully.";
        //        response.Data = new { Token = token, MeetingNumber = meeting.MeetingNumber }; // Trả về token và meetingNumber
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Error accepting meeting request!";
        //        response.Errors.Add(ex.Message);
        //    }

        //    return response;
        //}

        public async Task<BaseResponse> RejectMeetingRequestAsync(string bookingCode, int id)
        {
            var response = new BaseResponse();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.Queryable()
                            .Include(z => z.Booking)
                            .Where(z => z.Booking.BookingCode == bookingCode && z.Id == id && z.Status == ZoomMeeting.MeetingStatus.Requested)
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

        public async Task<BaseResponse> EndMeetingAsync(string bookingCode, int id)
        {
            var response = new BaseResponse();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.Queryable()
                                    .Include(z => z.Booking)
                                    .Where(z => z.Booking.BookingCode == bookingCode && z.Id == id)
                                    .FirstOrDefaultAsync();

                if (meeting == null)
                {
                    response.Message = "Meeting not found!";
                    return response;
                }

                if (!long.TryParse(meeting.MeetingNumber, out long zoomMeetingId))
                {
                    response.Message = "Invalid MeetingNumber format!";
                    return response;
                }

                var accessToken = await GetAccessTokenAsync();

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var zoomApiUrl = $"{_zoomSettings.ApiBaseUrl}meetings/{zoomMeetingId}";
                var result = await _httpClient.DeleteAsync(zoomApiUrl);

                if (!result.IsSuccessStatusCode)
                {
                    response.Message = $"Failed to end Zoom meeting!";
                    return response;
                }

                meeting.Status = ZoomMeeting.MeetingStatus.Ended;
                _unitOfWork.ZoomRepository.Update(meeting);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Zoom meeting ended successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error while ending meeting!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CreateMeetingScheduleAsync(string bookingCode, int providerId, List<DateTime> scheduledTime)
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

                var meetings = new List<ZoomMeeting>();
                
                foreach (var time in scheduledTime)
                {
                    var zoomMeeting = new ZoomMeeting
                    {
                        Topic = $"{decorService} Booking Request - Meeting",
                        StartTime = time,
                        CreateAt = DateTime.Now,
                        Status = ZoomMeeting.MeetingStatus.Requested,
                        BookingId = booking.Id,
                        AccountId = providerId
                    };

                    await _unitOfWork.ZoomRepository.InsertAsync(zoomMeeting);
                    meetings.Add(zoomMeeting);
                }            

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Meeting schedule created successfully.";
                response.Data = meetings;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error createing meeting schedule.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> SelectMeetingAsync(string bookingCode, int id)
        {
            var response = new BaseResponse();

            try
            {
                var meeting = await _unitOfWork.ZoomRepository.Queryable()
                                    .Include(z => z.Booking)
                                        .ThenInclude(b => b.DecorService)
                                    .Where(z => z.Booking.BookingCode == bookingCode && z.Id == id && z.Status == ZoomMeeting.MeetingStatus.Requested)
                                    .FirstOrDefaultAsync();

                if (meeting == null)
                {
                    response.Message = "No matching meeting schedule found!";
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
                meeting.MeetingNumber = zoomResponse.MeetingNumber;
                meeting.Password = zoomResponse.Password;
                meeting.StartUrl = zoomResponse.StartUrl;

                _unitOfWork.ZoomRepository.Update(meeting);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Meeting schedule selected.";
                response.Data = meeting;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error selecting meeting schedule.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> CancelMeetingAsync(int id)
        {
            var response = new BaseResponse();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.GetByIdAsync(id);

                if (meeting == null || meeting.Status != ZoomMeeting.MeetingStatus.Scheduled)
                {
                    response.Message = "Meeting not found or cannot be cancelled.";
                    return response;
                }

                var cancelled = await CancelMeetingAsync(meeting.MeetingNumber);

                if (!cancelled)
                {
                    response.Message = "Failed to cancel Zoom meeting.";
                    return response;
                }

                meeting.ZoomUrl = null;
                meeting.Duration = null;
                meeting.Status = ZoomMeeting.MeetingStatus.Requested;
                meeting.ResponseAt = DateTime.Now;
                meeting.MeetingNumber = null;
                meeting.Password = null;
                meeting.StartUrl = null;
                _unitOfWork.ZoomRepository.Update(meeting);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Meeting cancelled successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error cancelling meeting.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<PageResult<MeetingListResponse>>> GetMeetingForCustomerAsync(int accountId, ZoomFilterRequest request)
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

        public async Task<BaseResponse<PageResult<MeetingListResponse>>> GetMeetingForProviderAsync(int accountId, ZoomFilterRequest request)
        {
            var response = new BaseResponse<PageResult<MeetingListResponse>>();
            try
            {

                // Filter
                Expression<Func<ZoomMeeting, bool>> filter = meeting =>
                    meeting.Booking.DecorService.AccountId == accountId &&
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

        public async Task<BaseResponse<MeetingDetailResponse>> GetMeetingById(int id)
        {
            var response = new BaseResponse<MeetingDetailResponse>();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.GetByIdAsync(id);

                if (meeting == null)
                {
                    response.Message = "Meeting not found!";
                    return response;
                }

                response.Success = true;
                response.Message = "Meeting detail fetched successfully.";
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

        public async Task<BaseResponse<PageResult<MeetingScheduleReponse>>> GetProviderMeetingsForCustomerAsync(ZoomFilterRequest request)
        {
            var response = new BaseResponse<PageResult<MeetingScheduleReponse>>();
            try
            {
                var booking = await _unitOfWork.BookingRepository.Queryable()
                                            .Include(b => b.DecorService)
                                            .Where(b => b.BookingCode == request.BookingCode)
                                            .FirstOrDefaultAsync();

                var providerId = booking?.DecorService.AccountId;

                Expression<Func<ZoomMeeting, bool>> filter = meeting =>
                    meeting.Booking.DecorService.AccountId == providerId &&
                    meeting.StartTime >= DateTime.Today &&
                    (meeting.Status == ZoomMeeting.MeetingStatus.Scheduled || meeting.Status == ZoomMeeting.MeetingStatus.Started) &&
                    (!request.Status.HasValue || meeting.Status == request.Status);

                Expression<Func<ZoomMeeting, object>> orderByExpression = request.SortBy?.ToLower() switch
                {
                    "status" => meeting => meeting.Status,
                    _ => meeting => meeting.CreateAt
                };

                Expression<Func<ZoomMeeting, object>>[] includeProperties = {
                    m => m.Booking,
                    m => m.Booking.DecorService
                };

                (IEnumerable<ZoomMeeting> meetings, int totalCount) = await _unitOfWork.ZoomRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    includeProperties
                );

                var mappedMeetings = _mapper.Map<List<MeetingScheduleReponse>>(meetings);

                response.Success = true;
                response.Message = "Successfully retrieved all provider meetings for the customer.";
                response.Data = new PageResult<MeetingScheduleReponse>
                {
                    Data = mappedMeetings,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving meetings.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        //public async Task<BaseResponse<ZoomJoinInfoResponse>> GetZoomJoinInfo(int id)
        //{
        //    var response = new BaseResponse<ZoomJoinInfoResponse>();
        //    try
        //    {
        //        var meeting = await _unitOfWork.ZoomRepository.Queryable()
        //                    .Include(z => z.Booking)
        //                        .ThenInclude(b => b.Account)
        //                    .FirstOrDefaultAsync(z => z.Id == id && z.Status == ZoomMeeting.MeetingStatus.Scheduled);

        //        if (meeting == null || string.IsNullOrEmpty(meeting.ZoomUrl))
        //        {
        //            response.Message = "Meeting not found or not yet scheduled!";
        //            return response;
        //        }

        //        var accountId = GetCurrentAccountId();
        //        if (accountId == null || meeting.AccountId != accountId)
        //        {
        //            response.Message = "Unauthorized access.";
        //            return response;
        //        }

        //        var accessToken = await GetZoomAccessTokenAsync();

        //        if (string.IsNullOrWhiteSpace(accessToken))
        //        {
        //            response.Message = "Cannot get access token from Zoom!";
        //            return response;
        //        }

        //        var zoomMeetingData = new ZoomMeetingData();
        //        var zoomZakData = new ZoomTokenOnlyResponse();

        //        var apiUrl = $"{_configuration["Zoom:ApiBaseUrl"]}meetings/{meeting.MeetingNumber}";
        //        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl)
        //        {
        //            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        //        };

        //        var apiResponse = await _httpClient.SendAsync(request);
        //        var apiResponseString = await apiResponse.Content.ReadAsStringAsync();

        //        // Deserialize tk
        //        zoomMeetingData = JsonConvert.DeserializeObject<ZoomMeetingData>(apiResponseString);

        //        // 2. Gọi API lấy "zak"
        //        var zakRequest = new HttpRequestMessage(HttpMethod.Get, $"{_configuration["Zoom:ApiBaseUrl"]}users/me/token?type=zak")
        //        {
        //            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        //        };

        //        var zakResponse = await _httpClient.SendAsync(zakRequest);
        //        var zakResponseString = await zakResponse.Content.ReadAsStringAsync();

        //        // Deserialize zak
        //        zoomZakData = JsonConvert.DeserializeObject<ZoomTokenOnlyResponse>(zakResponseString);

        //        var fullName = $"{meeting.Booking.Account?.FirstName} {meeting.Booking.Account?.LastName}";

        //        // Parse Meeting Number từ ZoomUrl
        //        var meetingNumber = ExtractMeetingNumber(meeting.ZoomUrl);

        //        var signature = GenerateZoomSignature(meetingNumber, 0);

        //        var joinInfo = new ZoomJoinInfoResponse
        //        {
        //            ApiKey = _configuration["Zoom:AccountId"], // Use AccountId from config
        //            Signature = signature,
        //            MeetingNumber = meeting.MeetingNumber,
        //            Password = meeting.Password,
        //            UserName = fullName,
        //            UserEmail = meeting.Booking.Account.Email,
        //            Tk = zoomMeetingData.Tk,
        //            Zak = zoomZakData.Zak,
        //            Role = 0,
        //            ZoomUrl = meeting.ZoomUrl
        //        };

        //        response.Success = true;
        //        response.Message = "Success";
        //        response.Data = joinInfo;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Error retrieving join info!";
        //        response.Errors.Add(ex.Message);
        //    }

        //    return response;
        //}

        private string ExtractMeetingNumber(string zoomUrl)
        {
            var uri = new Uri(zoomUrl);
            var segments = uri.Segments;
            // segments có dạng: ["/", "j/", "12345678901"]
            var meetingSegment = segments.FirstOrDefault(s => s.All(char.IsDigit));

            if (string.IsNullOrEmpty(meetingSegment))
            {
                // fallback nếu URL dạng "j/12345678901"
                meetingSegment = segments.LastOrDefault()?.Trim('/');
            }

            return meetingSegment;
        }

        private async Task<string> GetZoomAccessTokenAsync()
        {
            var zoomConfig = _configuration.GetSection("Zoom");
            var clientId = zoomConfig["ClientId"];
            var clientSecret = zoomConfig["ClientSecret"];
            var tokenEndpoint = zoomConfig["TokenEndpoint"];

            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestBody));
            var responseString = await response.Content.ReadAsStringAsync();

            // Deserialize to extract access token
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);

            return tokenResponse?.AccessToken;
        }

        //public string GenerateZoomSignature(string meetingNumber, int role)
        //{
        //    var ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds - 30000;

        //    var message = $"{_zoomSettings.ClientId}{meetingNumber}{ts}{role}";
        //    var encoding = new UTF8Encoding();
        //    var keyByte = encoding.GetBytes(_zoomSettings.ClientSecret);
        //    var messageBytes = encoding.GetBytes(message);
        //    using (var hmacsha256 = new HMACSHA256(keyByte))
        //    {
        //        var hashmessage = hmacsha256.ComputeHash(messageBytes);
        //        var hash = Convert.ToBase64String(hashmessage);
        //        var token = $"{_zoomSettings.ClientId}.{meetingNumber}.{ts}.{role}.{hash}";
        //        var bytes = Encoding.UTF8.GetBytes(token);
        //        return Convert.ToBase64String(bytes);
        //    }
        //}

        //public async Task<BaseResponse<ZoomJoinInfoResponse>> GetZoomJoinInfo(int id)
        //{
        //    var response = new BaseResponse<ZoomJoinInfoResponse>();
        //    try
        //    {
        //        var meeting = await _unitOfWork.ZoomRepository.Queryable()
        //                    .Include(z => z.Booking)
        //                        .ThenInclude(b => b.Account)
        //                    .FirstOrDefaultAsync(z => z.Id == id && z.Status == ZoomMeeting.MeetingStatus.Scheduled);

        //        if (meeting == null || string.IsNullOrEmpty(meeting.ZoomUrl))
        //        {
        //            response.Message = "Meeting not found or not yet scheduled!";
        //            return response;
        //        }

        //        var zoomConfig = _configuration.GetSection("Zoom");
        //        var sdkKey = zoomConfig["ClientId"];
        //        var sdkSecret = zoomConfig["ClientSecret"];

        //        int role = 0;

        //        var signature = GenerateMeetingSdkSignature(sdkKey, sdkSecret, meeting.MeetingNumber, role);

        //        var userName = $"{meeting.Booking.Account?.FirstName} {meeting.Booking.Account?.LastName}";

        //        var joinInfo = new ZoomJoinInfoResponse
        //        {
        //            SdkKey = sdkKey,
        //            Signature = signature,
        //            MeetingNumber = meeting.MeetingNumber,
        //            UserName = userName,
        //            Password = meeting.Password,
        //            Role = role
        //        };

        //        response.Success = true;
        //        response.Message = "Success";
        //        response.Data = joinInfo;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Error retrieving join info!";
        //        response.Errors.Add(ex.Message);
        //    }

        //    return response;
        //}

        public static string GenerateMeetingSdkSignature(string sdkKey, string sdkSecret, string meetingNumber, int role)
        {
            var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 30;
            var exp = iat + 60 * 60 * 2;
            var tokenExp = exp;

            var payload = new Dictionary<string, object>
            {
                { "appKey", sdkKey },
                { "sdkKey", sdkKey },
                { "mn", meetingNumber },
                { "role", role },
                { "iat", iat },
                { "exp", exp },
                { "tokenExp", tokenExp }
            };

            var header = new Dictionary<string, object>
            {
                { "alg", "HS256" },
                { "typ", "JWT" }
            };

            string headerJson = System.Text.Json.JsonSerializer.Serialize(header);
            string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

            string encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            string encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            string dataToSign = $"{encodedHeader}.{encodedPayload}";

            var signatureBytes = HmacSha256(Encoding.UTF8.GetBytes(dataToSign), Encoding.UTF8.GetBytes(sdkSecret));
            var encodedSignature = Base64UrlEncode(signatureBytes);

            return $"{encodedHeader}.{encodedPayload}.{encodedSignature}";
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static byte[] HmacSha256(byte[] data, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        //public async Task<BaseResponse<ZoomJoinInfoResponse>> GetZoomJoinInfo(int id)
        //{
        //    var response = new BaseResponse<ZoomJoinInfoResponse>();
        //    try
        //    {
        //        var meeting = await _unitOfWork.ZoomRepository.Queryable()
        //                    .Include(z => z.Booking)
        //                        .ThenInclude(b => b.Account)
        //                    .FirstOrDefaultAsync(z => z.Id == id && z.Status == ZoomMeeting.MeetingStatus.Scheduled);

        //        if (meeting == null || string.IsNullOrEmpty(meeting.Token))
        //        {
        //            response.Message = "Meeting not found or not yet scheduled!";
        //            return response;
        //        }

        //        var accountId = GetCurrentAccountId();

        //        if (accountId == null || meeting.AccountId != accountId)
        //        {
        //            response.Message = "Unauthorized access.";
        //            return response;
        //        }

        //        int role = 0;

        //        // 1. Tạo token cho Zoom Video SDK
        //        var token = await GenerateVideoSdkToken(meeting.Topic);

        //        // 2. Lấy tên người dùng để hiện trên Zoom
        //        var userName = $"{meeting.Booking.Account?.FirstName} {meeting.Booking.Account?.LastName}";

        //        var joinInfo = new ZoomJoinInfoResponse
        //        {
        //            Topic = meeting.Topic,
        //            Token = token,
        //            UserName = userName,
        //            Password = meeting.Password
        //        };

        //        response.Success = true;
        //        response.Message = "Success";
        //        response.Data = joinInfo;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = "Error retrieving join info!";
        //        response.Errors.Add(ex.Message);
        //    }

        //    return response;
        //}

        public async Task<string> GenerateVideoSdkToken(string topic)
        {
            var zoomConfig = _configuration.GetSection("Client");
            var sdkKey = zoomConfig["SdkKey"];
            var sdkSecret = zoomConfig["SdkSecret"];

            var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 30;
            var expireAt = issuedAt + 60 * 60 * 2;

            var zoom = await _unitOfWork.ZoomRepository.Queryable()
                                    .Where(z => z.Topic == topic)
                                    .FirstOrDefaultAsync();

            var payload = new Dictionary<string, object>
            {
                { "app_key", sdkKey },
                { "role_type", 0 },
                { "tpc", zoom.Topic },
                { "iat", issuedAt },
                { "exp", expireAt }
            };

            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(sdkSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var header = new JwtHeader(credentials);

            JwtPayload jwtPayload = new JwtPayload();
            foreach (var pair in payload)
            {
                jwtPayload[pair.Key] = pair.Value;
            }

            var token = new JwtSecurityToken(header, jwtPayload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int? GetCurrentAccountId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return null;

            var accountIdClaim = user.Claims.FirstOrDefault(c => c.Type == "accountId" || c.Type == ClaimTypes.NameIdentifier);
            if (accountIdClaim == null)
                return null;

            return int.TryParse(accountIdClaim.Value, out var id) ? id : null;
        }
    }
}
