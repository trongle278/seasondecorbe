﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateBookingRequest
    {
        public int DecorServiceId { get; set; }
        public int AddressId { get; set; }
        public DateTime SurveyDate { get; set; } // Ngày khảo sát
        public TimeSpan SurveyTime { get; set; } // Giờ khảo sát
    }

    /// <summary>
    /// Request để thêm tracking tiến độ thi công vào booking
    /// </summary>
    public class TrackingRequest
    {
        [Required]
        [StringLength(255)]
        public string Stage { get; set; }

        public DateTime? PlannedDate { get; set; }

        public DateTime? ActualDate { get; set; }

        public string ImageUrls { get; set; }
    }

    public class CreateQuotationRequest
    {
        [Required(ErrorMessage = "Materials list is required")]
        [MinLength(1, ErrorMessage = "At least one material item is required")]
        public List<MaterialItemRequest> Materials { get; set; }
        [Required(ErrorMessage = "Construction tasks list is required")]
        [MinLength(1, ErrorMessage = "At least one construction task is required")]
        public List<ConstructionItemRequest> ConstructionTasks { get; set; }
        public decimal DepositPercentage { get; set; }
    }

    public class MaterialItemRequest
    {
        public string MaterialName { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public MaterialDetail.MaterialCategory Category { get; set; }
    }

    public class ConstructionItemRequest
    {
        public string TaskName { get; set; }
        public decimal Cost { get; set; }
        public string Unit { get; set; }
    }

    public class UpdateTrackingRequest
    {
        public int TrackingId { get; set; }
        public string? Note { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class RejectBookingRequest
    {
        public string Reason { get; set; }
    }
}
