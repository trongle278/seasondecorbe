using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLogicLayer.ModelRequest
{
    public class TrackingRequest
    {
        public string Task { get; set; }
        public string? Note { get; set; }
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }

    public class UpdateTrackingRequest
    {

        public string Note { get; set; }

        public List<ImageReplacement> ImageReplacements { get; set; } = new List<ImageReplacement>();
        public List<IFormFile> NewImages { get; set; } = new List<IFormFile>();
    }

    public class ImageReplacement
    {
        [Required]
        public string OldImageUrl { get; set; }  // URL của ảnh cũ cần thay thế

        [Required]
        public IFormFile NewImage { get; set; }  // File ảnh mới
    }
}
