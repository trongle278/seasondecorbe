﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelRequest
{
    public class BecomeProviderRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Name can only contain letters and spaces")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Bio is required")]
        public string Bio { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must start with 0 and contain exactly 10 digits")]
        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }
        
        [Required(ErrorMessage = "YearsOfExperience is required")]
        public int? YearsOfExperience { get; set; }       //Kinh nghiệm 
        
        [Required(ErrorMessage = "PastWorkPlaces is required")]
        public string? PastWorkPlaces { get; set; }      // Đã từng hoạt động ở đâu?
        
        [Required(ErrorMessage = "PastProjects is required")]
        public string? PastProjects { get; set; }        // Các dự án đã từng làm
        
        [Required(ErrorMessage = "SkillId is required")]
        public int SkillId { get; set; }

        //[Required(ErrorMessage = "DecorationStyleId is required")]
        //public int DecorationStyleId { get; set; }

        public List<IFormFile> CertificateImages { get; set; }
    }

    public class UpdateProviderRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Name can only contain letters and spaces")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string Bio { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must start with 0 and contain exactly 10 digits")]
        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }
    }
}
