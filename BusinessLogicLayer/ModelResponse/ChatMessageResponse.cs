using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class ChatMessageResponse
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string Message { get; set; }
        public DateTime SentTime { get; set; }
        public bool IsRead { get; set; }

        // Mở rộng: danh sách file đính kèm
        public List<ChatFileResponse> Files { get; set; } = new List<ChatFileResponse>();
    }

    public class ChatFileResponse
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
