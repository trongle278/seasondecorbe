using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DataAccessObject.Models;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;

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
        public HomeDecorAutoMapperProfile()
        {
            AccountProfile();
        }

        private void AccountProfile()
        {
            CreateMap<Account, AccountDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId));

            CreateMap<CreateAccountRequest, Account>();
            CreateMap<UpdateAccountRequest, Account>();
        }
    }
}
