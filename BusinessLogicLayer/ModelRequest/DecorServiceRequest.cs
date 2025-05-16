using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class CreateDecorServiceRequest
    {
        [Required(ErrorMessage = "Style is required")]
        public string Style { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Province is required")]
        public string Sublocation { get; set; }

        [Required(ErrorMessage = "DecorCategoryId is required")]
        public int DecorCategoryId { get; set; }

        [Required(ErrorMessage = "StartDate is required")]
        public DateTime StartDate { get; set; }
        
        // Danh sách Tag Season để gán vào dịch vụ
        public List<int> SeasonIds { get; set; } = new List<int>();
        public List<string> ThemeColorNames { get; set; } // mới: nhập tay tên màu
        public List<int> StyleIds { get; set; }  // mới: chọn từ danh sách đã seed      
        public List<int> OfferingIds { get; set; }

        // Danh sách file ảnh đính kèm, tối đa 5 ảnh
        public List<IFormFile> Images { get; set; }
    }

    public class UpdateDecorServiceRequest
    {
        [Required(ErrorMessage = "Style is required")]
        public string Style { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Province is required")]
        public string Sublocation { get; set; }

        [Required(ErrorMessage = "DecorCategoryId is required")]
        public int DecorCategoryId { get; set; }

        // Danh sách SeasonIds để cập nhật
        public List<int> SeasonIds { get; set; } = new List<int>();
    }

    public class ChangeStartDateRequest
    {
        [Required(ErrorMessage = "StartDate is required")]
        public DateTime StartDate { get; set; }
    }
}

