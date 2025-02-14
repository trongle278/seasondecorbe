using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class ChatFile
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Chat Chat { get; set; }
    }
}
