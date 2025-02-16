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
            ProviderProfile();
            TicketTypeProfile();
            TicketProfile();
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

        private void ProviderProfile()
        {
            CreateMap<BecomeProviderRequest, Provider>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.Bio))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.JoinedDate, opt => opt.MapFrom(src => src.JoinedDate))
                .ForMember(dest => dest.IsProvider, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.Account, opt => opt.MapFrom(src => new Account
                {
                    Phone = src.Phone,
                    Address = src.Address
                }));
        }

        private void TicketTypeProfile()
        {
            // Mapping from TicketTypeRequest to TicketType
            CreateMap<TicketTypeRequest, TicketType>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));

            // Mapping from TicketType (entity) to TicketTypeDTO (response)
            CreateMap<TicketType, TicketTypeResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
        }

        private void TicketProfile()
        {
            // Mapping từ Support (entity) sang SupportResponse (DTO)
            CreateMap<Support, SupportResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreateAt))
                // Chuyển enum TicketStatus thành string
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.TicketStatus.ToString()))
                .ForMember(dest => dest.TicketTypeId, opt => opt.MapFrom(src => src.TicketTypeId))
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
                // Mapping cho danh sách reply (sử dụng mapping đã định nghĩa bên dưới)
                .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.TicketReplies))
                // Lấy URL từ các TicketAttachment của ticket chính
                .ForMember(dest => dest.AttachmentUrls, opt => opt.MapFrom(src => src.TicketAttachments.Select(a => a.FileUrl).ToList()));

            // Mapping từ TicketReply (entity) sang SupportReplyResponse (DTO)
            CreateMap<TicketReply, SupportReplyResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SupportId, opt => opt.MapFrom(src => src.SupportId))
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(src => src.AccountId))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreateAt))
                // Lấy URL từ các file đính kèm của reply
                .ForMember(dest => dest.AttachmentUrls, opt => opt.MapFrom(src => src.TicketAttachments.Select(a => a.FileUrl).ToList()));
        }
    }
}
