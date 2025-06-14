﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Account
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Slug { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Avatar { get; set; }
        public bool IsDisable { get; set; }
        public bool IsVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpiry { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiry { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorToken { get; set; }
        public DateTime? TwoFactorTokenExpiry { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        public Cart Cart { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Follow> Followers { get; set; }
        public virtual ICollection<Follow> Followings { get; set; }
        public virtual ICollection<Support> Supports { get; set; }
        public virtual ICollection<TicketReply> TicketReplies { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<DecorService> DecorServices { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<RelatedProduct> RelatedProducts { get; set; }
        public virtual ICollection<FavoriteProduct> FavoriteProducts { get; set; }
        public virtual ICollection<FavoriteService> FavoriteServices { get; set; } = new List<FavoriteService>();
        public virtual Wallet Wallet { get; set; }

        // Thông tin Provider
        public string? BusinessName { get; set; }
        public string? Bio { get; set; }
        public string? BusinessAddress { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool? IsProvider { get; set; }
        public bool? ProviderVerified { get; set; }

        // Thông tin chuyên môn
        public int? YearsOfExperience { get; set; }
        public string? PastWorkPlaces { get; set; }      // Đã từng hoạt động ở đâu?
        public string? PastProjects { get; set; }        // Các dự án đã từng làm
        public DateTime? ApplicationCreateAt { get; set; }

        public int? SkillId { get; set; }//Kĩ năng chuyên môn
        public virtual Skill Skill { get; set; }

        //public int? DecorationStyleId { get; set; }//Phong cách trang trí 
        //public virtual DecorationStyle DecorationStyle { get; set; }
        // Chứng chỉ
        public ICollection<CertificateImage> CertificateImages { get; set; }

        public string? Location { get; set; }
        public string? ProvinceCode { get; set; }
        public AccountStatus ProviderStatus { get; set; } // Trạng thái tài khoản
        public enum AccountStatus
        {
            Idle,    // Đang rảnh (có thể nhận job)           
            Busy        // Đang bận (có job)          
        }

        public int Reputation { get; set; }     
        
        public ICollection<ZoomMeeting> ZoomMeetings { get; set; }
        public virtual ICollection<BookingForm>? BookingForms { get; set; }
    }
}
