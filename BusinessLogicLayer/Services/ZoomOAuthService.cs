using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Utilities.Zoom;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class ZoomOAuthService : IZoomOAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ZoomOAuthOptions _options;
        private readonly HttpClient _httpClient;
        private readonly IUnitOfWork _unitOfWork;

        public ZoomOAuthService(IConfiguration configuration, IOptions<ZoomOAuthOptions> options, HttpClient httpClient, IUnitOfWork unitOfWork)
        {
            _configuration = configuration;
            _options = options.Value;
            _httpClient = httpClient;
            _unitOfWork = unitOfWork;

            _httpClient.BaseAddress = new Uri(_options.ApiBaseUrl);
        }

        public async Task<BaseResponse<ZoomTokenResponse>> GetAccessTokenAsync(string code)
        {
            var response = new BaseResponse<ZoomTokenResponse>();
            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

                var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", _options.RedirectUri }
                });

                var httpResponse = await _httpClient.SendAsync(request);
                var content = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    response.Message = $"Failed to get access token: {errorContent}";
                    return response;
                }

                var tokenResponse = JsonConvert.DeserializeObject<ZoomTokenResponse>(content);

                response.Success = true;
                response.Message = "Get access token successfully.";
                response.Data = tokenResponse;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error while getting access token!";
                response.Errors.Add(ex.Message);
            }
            
            return response;
        }

        public async Task<BaseResponse<ZoomTokenResponse>> RefreshAccessTokenAsync(string refreshToken)
        {
            var response = new BaseResponse<ZoomTokenResponse>();
            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

                var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                });

                var httpResponse = await _httpClient.SendAsync(request);
                var content = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    response.Message = $"Failed to refresh token: {errorContent}";
                    return response;
                }
                
                var tokenResponse = JsonConvert.DeserializeObject<ZoomTokenResponse>(content);

                response.Success = true;
                response.Message = "Get refresh token successfully.";
                response.Data = tokenResponse;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error while getting refresh token!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<ZoomMeetingResponse> CreateMeetingAsync(string accessToken, ZoomMeetingRequest request)
        {
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

        public async Task<BaseResponse> AcceptMeetingRequestAsync(string bookingCode, int id, ZoomOAuthRequest request)
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

                var accessToken = request.AccessToken;

                var zoomResponse = await CreateMeetingAsync(accessToken, zoomRequest);

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

        public async Task<BaseResponse> EndMeetingAsync(string bookingCode, int id, ZoomOAuthRequest request)
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

                var accessToken = request.AccessToken;

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var zoomApiUrl = $"{_options.ApiBaseUrl}meetings/{zoomMeetingId}";
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

        public async Task<BaseResponse<ZoomSdkJoinInfoResponse>> GetZoomJoinInfo(int id)
        {
            var response = new BaseResponse<ZoomSdkJoinInfoResponse>();
            try
            {
                var meeting = await _unitOfWork.ZoomRepository.Queryable()
                            .Include(z => z.Booking)
                                .ThenInclude(b => b.Account)
                            .FirstOrDefaultAsync(z => z.Id == id && z.Status == ZoomMeeting.MeetingStatus.Scheduled);

                if (meeting == null || string.IsNullOrEmpty(meeting.ZoomUrl))
                {
                    response.Message = "Meeting not found or not yet scheduled!";
                    return response;
                }

                var zoomConfig = _configuration.GetSection("Zoom");
                var sdkKey = zoomConfig["ClientId"];
                var sdkSecret = zoomConfig["ClientSecret"];

                int role = 0;

                var signature = GenerateMeetingSdkSignature(sdkKey, sdkSecret, meeting.MeetingNumber, role);

                var userName = $"{meeting.Booking.Account?.FirstName} {meeting.Booking.Account?.LastName}";

                var joinInfo = new ZoomSdkJoinInfoResponse
                {
                    SdkKey = sdkKey,
                    Signature = signature,
                    MeetingNumber = meeting.MeetingNumber,
                    UserName = userName,
                    Password = meeting.Password,
                    Role = role
                };

                response.Success = true;
                response.Message = "Success";
                response.Data = joinInfo;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving join info!";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

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
    }
}
