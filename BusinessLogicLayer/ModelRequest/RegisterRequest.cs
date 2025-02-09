using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        [Range(typeof(DateTime), "1900-01-01", "2006-12-31", ErrorMessage = "You must be at least 18 years old")]  // Sửa lại range
        [DefaultValue("2000-01-01")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public bool Gender { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Phone number must start with 0 and have 10 digits")]
        [DefaultValue("0123456789")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [RegularExpression(@"^[a-zA-Z0-9\s,.-]*$", ErrorMessage = "Address contains invalid characters")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        [DefaultValue("123 Main Street")]
        public string Address { get; set; }
    }

    public class VerifyEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string OTP { get; set; }
    }
}
