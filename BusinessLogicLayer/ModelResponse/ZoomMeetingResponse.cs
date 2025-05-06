using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BusinessLogicLayer.ModelResponse
{
    public class ZoomMeetingResponse
    {
        public string Id { get; set; }

        [JsonProperty("join_url")]
        public string JoinUrl { get; set; }

        [JsonProperty("start_url")]
        public string StartUrl { get; set; }

        [JsonProperty("id")]
        public string MeetingNumber { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    public class ZoomTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class ZoomMeetingData
    {
        [JsonProperty("tk")]
        public string Tk { get; set; }
    }

    public class ZoomTokenOnlyResponse
    {
        [JsonProperty("token")]
        public string Zak { get; set; }
    }

    public class MeetingListResponse
    {
        public int Id { get; set; }
        public string Topic { get; set; }
        public DateTime StartTime { get; set; }
        public string? ZoomUrl { get; set; }
        public DateTime CreateAt { get; set; }
        public string? MeetingNumber { get; set; }
        public enum MeetingStatus
        {
            Requested,
            Scheduled,
            Started,
            Ended,
            Rejected
        }
        public MeetingStatus Status { get; set; }
    }

    public class MeetingDetailResponse
    {
        public int id { get; set; }
        public string Topic { get; set; }
        public DateTime StartTime { get; set; }
        public int? Duration { get; set; }
        public string? ZoomUrl { get; set; }
        public enum MeetingStatus
        {
            Requested,
            Scheduled,
            Started,
            Ended,
            Rejected
        }
        public MeetingStatus Status { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? ResponseAt { get; set; }
        public int BookingId { get; set; }
        public int AccountId { get; set; }
        public string? MeetingNumber { get; set; }
    }

    public class ZoomJoinInfoResponse // Zoom Video
    {
        public string Topic { get; set; }
        public string Token { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    //public class ZoomJoinInfoResponse // Zoom Meeting
    //{
    //    public string SdkKey { get; set; }
    //    public string Signature { get; set; }
    //    public string MeetingNumber { get; set; }
    //    public string UserName { get; set; }
    //    public string Password { get; set; }
    //    public int Role { get; set; }
    //}

    //public class ZoomJoinInfoResponse // Zoom
    //{
    //    public string ApiKey { get; set; } // SdkKey
    //    public string Signature { get; set; }
    //    public string MeetingNumber { get; set; }
    //    public string Password { get; set; }
    //    public string UserName { get; set; }
    //    public string UserEmail { get; set; }
    //    public string Tk { get; set; }
    //    public string Zak { get; set; }
    //    public int Role { get; set; }
    //    public string ZoomUrl { get; set; }
    //}
}
