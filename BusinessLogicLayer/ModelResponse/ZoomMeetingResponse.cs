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
        //[JsonProperty("join_url")]
        //public string JoinUrl { get; set; }

        //[JsonProperty("start_url")]
        //public string StartUrl { get; set; }

        //[JsonProperty("id")]
        //public string MeetingId { get; set; }
        public string Id { get; set; }
        public string JoinUrl { get; set; }
        public string StartUrl { get; set; }
        public string Topic { get; set; }
        public int Duration { get; set; }
        public DateTime StartTime { get; set; }
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

    public class MeetingListResponse
    {
        public int id { get; set; }
        public string Topic { get; set; }
        public DateTime StartTime { get; set; }
        public string? ZoomUrl { get; set; }
        public DateTime CreateAt { get; set; }
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
    }
}
