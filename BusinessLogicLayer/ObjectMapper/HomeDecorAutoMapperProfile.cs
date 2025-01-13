using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ObjectMapper
{
    public class HomeDecorAutoMapperProfile : Profile
    {
        /*
        public ECommerceAutoMapperProfile()
        {
            ProductProfile();
            CategoryProfile();
            ManufacturerProfile();
        }

        private void ProductProfile()
        {
            CreateMap<Product, ProductResponse>().ReverseMap();
            CreateMap<Product, ProductAddRequest>().ReverseMap();
            CreateMap<Product, ProductUpdateRequest>().ReverseMap();
        }

        private void CategoryProfile()
        {
            CreateMap<DataAccessObject.Models.Category, CategoryRequest>().ReverseMap();
            CreateMap<DataAccessObject.Models.Category, CategoryResponse>().ReverseMap();
        }

        private void ManufacturerProfile()
        {
            CreateMap<DataAccessObject.Models.Manufacturer, ManufacturerRequest>().ReverseMap();
            CreateMap<DataAccessObject.Models.Manufacturer, ManufacturerResponse>().ReverseMap();
        }
        */
    }
}
