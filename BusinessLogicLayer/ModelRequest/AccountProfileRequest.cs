using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateAccountRequest
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
        [Range(typeof(DateTime), "1900-01-01", "2006-12-31", ErrorMessage = "You must be at least 18 years old")]
        [DefaultValue("2000-01-01")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public bool? Gender { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        public string? Phone { get; set; }
    }

    public class UpdateAccountRequest
    {
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
        [DefaultValue("johnweak")]
        public string Slug { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        [Range(typeof(DateTime), "1900-01-01", "2006-12-31", ErrorMessage = "You must be at least 18 years old")]
        [DefaultValue("2000-01-01")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public bool? Gender { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        public string? Phone { get; set; }
    }
}
