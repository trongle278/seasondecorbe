using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse
{
    public class RejectedApplicationResponse
    {
        public string Reason { get; set; }
        public DateTime RejectedAt { get; set; }
    }

    public class PendingProviderResponse
    {
        public int AccountId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Avatar { get; set; }
        public string? Phone { get; set; }
        public string? BusinessName { get; set; }
        public string? Bio { get; set; }
        public string? BusinessAddress { get; set; }
        public bool? IsProvider { get; set; }
        public bool? ProviderVerified { get; set; }

        public string? SkillName { get; set; }  // Tên kỹ năng
        //public string? DecorationStyleName { get; set; }  // Phong cách trang trí
        public int? YearsOfExperience { get; set; }  // Số năm kinh nghiệm
        public string? PastWorkPlaces { get; set; }  // Các công ty đã làm việc
        public string? PastProjects { get; set; }  // Các dự án đã thực hiện
        public List<string> CertificateImageUrls { get; set; }
    }

    public class VerifiedProviderResponse
    {
        public int AccountId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Avatar { get; set; }
        public string? Phone { get; set; }
        public string? BusinessName { get; set; }
        public string? Bio { get; set; }
        public string? BusinessAddress { get; set; }
        public bool? IsProvider { get; set; }
        public bool? ProviderVerified { get; set; }

        public string? SkillName { get; set; }  // Tên kỹ năng
        public string? DecorationStyleName { get; set; }  // Phong cách trang trí
        public int? YearsOfExperience { get; set; }  // Số năm kinh nghiệm
        public string? PastWorkPlaces { get; set; }  // Các công ty đã làm việc
        public string? PastProjects { get; set; }  // Các dự án đã thực hiện
        public List<string> CertificateImageUrls { get; set; }
    }
}
