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

    public class DecorServiceBySlugResponse : BaseResponse<List<DecorServiceDTO>>
    {
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
        public string Sublocation { get; set; }
        public DateTime CreateAt { get; set; }
        public int AccountId { get; set; }
        public int DecorCategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime StartDate { get; set; }
        public int Status { get; set; }
        public bool IsBooked { get; set; }
        public int FavoriteCount { get; set; }
        public List<DecorImageResponse> Images { get; set; } = new List<DecorImageResponse>();
        public List<SeasonResponse> Seasons { get; set; } = new List<SeasonResponse>();
        public ProviderResponse Provider { get; set; }
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
        public string? Sublocation { get; set; }
        public string? CategoryName { get; set; }  // Tìm theo tên danh mục
        public List<string>? SeasonNames { get; set; }  // Tìm theo tên mùa

        public List<string>? DesignNames { get; set; }
    }

    /// <summary>
    /// ///////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    public class DecorServiceByIdResponse : BaseResponse
    {
        public DecorServiceById Data { get; set; }
    }
    public class DecorServiceById
    {
        public int Id { get; set; }
        public string Style { get; set; }
        public double? BasePrice { get; set; }
        public string Description { get; set; }
        public string Sublocation { get; set; }
        public DateTime CreateAt { get; set; }
        public int AccountId { get; set; }
        public int DecorCategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime StartDate { get; set; }
        public int Status { get; set; }
        public bool IsBooked { get; set; }
        public int FavoriteCount { get; set; }
        public List<DecorImageResponse> Images { get; set; } = new List<DecorImageResponse>();
        public List<SeasonResponse> Seasons { get; set; } = new List<SeasonResponse>();

        public List<ThemeColorResponse> ThemeColors { get; set; }
        public List<DesignResponse> Styles { get; set; }
        public List<OfferingResponse> Offerings { get; set; }


        public ProviderResponse Provider { get; set; }
        public List<DecorServiceReviewResponse> Reviews { get; set; } = new();
    }

    public class DecorServiceReviewResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Avatar { get; set; }
        public int Rate { get; set; }
        public string Comment { get; set; }
        public string CreateAt { get; set; }
        public List<DecorServiceReviewImageResponse> ReviewImages { get; set; }

        public class DecorServiceReviewImageResponse
        {
            public int Id { get; set; }
            public string ImageUrl { get; set; }
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///

    public class ThemeColorResponse
    {
        public int Id { get; set; }
        public string ColorCode { get; set; }
    }

    public class DesignResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OfferingResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class DecorServiceDetailsResponse
    {
        public List<ThemeColorResponse> ThemeColors { get; set; }
        public List<DesignResponse> DecorationStyles { get; set; }
    }

    public class OfferingAndDesignResponse
    {
        public List<OfferingResponse> Offerings { get; set; }
        public List<DesignResponse> DecorationStyles { get; set; }
    }
}
