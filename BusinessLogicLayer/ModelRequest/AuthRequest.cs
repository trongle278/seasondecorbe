using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [DefaultValue("user@gmail.com")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        [DefaultValue("123#Example")]
        public string Password { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "First name can only contain letters and spaces")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        [DefaultValue("John")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Last name can only contain letters and spaces")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        [DefaultValue("Weak")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Slug is required")]
        [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug can only contain letters and numbers")]
        [DefaultValue("johndoe")]
        public string Slug { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        [Range(typeof(DateTime), "1900-01-01", "2006-12-31", ErrorMessage = "You must be at least 18 years old")]  // Sửa lại range
        [DefaultValue("2000-01-01")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public bool Gender { get; set; }
    }

    public class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [DefaultValue("user@gmail.com")]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP must contain only numbers")]
        public string OTP { get; set; }
    }

    public class VerifyEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string OTP { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [DefaultValue("user@gmail.com")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        [DefaultValue("123#Example")]
        public string Password { get; set; }
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP must contain only numbers")]
        public string OTP { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
        public string NewPassword { get; set; }
    }

    public class Toggle2FARequest
    {
        [Required]
        public bool Enable { get; set; }
    }
}
