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
        public HomeDecorAutoMapperProfile()
        {
            AccountProfile();
            DecorCategoryProfile();
            RoleCategoryProfile();
            DecoratorProfile();
        }

        private void AccountProfile()
        {
            CreateMap<Account, AccountDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId));

            CreateMap<CreateAccountRequest, Account>();
            CreateMap<UpdateAccountRequest, Account>();
        }

        private void DecorCategoryProfile(){
            CreateMap<DecorCategory, DecorCategoryDTO>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));

            CreateMap<DecorCategoryRequest, DecorCategory>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));
        }

        private void RoleCategoryProfile()
        {
            CreateMap<Role, RoleDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.RoleName));

            CreateMap<CreateRoleRequest, Role>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.RoleName));

            CreateMap<UpdateRoleRequest, Role>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.RoleName));
        }

        private void DecoratorProfile()
        {
            CreateMap<BecomeDecoratorRequest, Decorator>()
                .ForMember(dest => dest.Nickname, opt => opt.MapFrom(src => src.Nickname))
                .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.Bio))
                .ForMember(dest => dest.YearsOfExperience, opt => opt.MapFrom(src => src.YearsOfExperience))
                .ForMember(dest => dest.SeasonalSpecialties, opt => opt.MapFrom(src => src.SeasonalSpecialties))
                .ForMember(dest => dest.PortfolioURL, opt => opt.MapFrom(src => src.PortfolioURL))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Common.Enums.DecoratorApplicationStatus.Pending));

            CreateMap<Decorator, DecoratorResponse>()
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Success, opt => opt.Ignore())
                .ForMember(dest => dest.Message, opt => opt.Ignore())
                .ForMember(dest => dest.Errors, opt => opt.Ignore());
        }
    }
}
