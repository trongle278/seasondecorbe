using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse
{
    public class DecorServiceResponse : BaseResponse
    {
        public DecorServiceDTO Data { get; set; }
    }

    public class DecorServiceListResponse : BaseResponse
    {
        public List<DecorServiceDTO> Data { get; set; }
    }

    public class DecorServiceDTO
    {
        public int Id { get; set; }
        public string Style { get; set; }
        public double? BasePrice { get; set; }
        public string Description { get; set; }
        public string Province { get; set; }
        public DateTime CreateAt { get; set; }
        public int AccountId { get; set; }
        public int DecorCategoryId { get; set; }
        public string CategoryName { get; set; }
        public int FavoriteCount { get; set; }
        public List<string> ImageUrls { get; set; }
        public List<DecorImageResponse> Images { get; set; } = new List<DecorImageResponse>();
        public List<SeasonResponse> Seasons { get; set; } = new List<SeasonResponse>();
    }

    public class DecorImageResponse
    {
        public int Id { get; set; }
        public string ImageURL { get; set; }
    }

    public class SeasonResponse
    {
        public int Id { get; set; }
        public string SeasonName { get; set; }
    }

    public class SearchDecorServiceRequest
    {
        public string? Style { get; set; }
        public string? Province { get; set; }
        public string? CategoryName { get; set; }  // Tìm theo tên danh mục
        public List<string>? SeasonNames { get; set; }  // Tìm theo tên mùa
    }
}
