﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DataAccessObject.Models;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelRequest.Cart;
using BusinessLogicLayer.ModelRequest.Order;
using BusinessLogicLayer.ModelRequest.Product;
using BusinessLogicLayer.ModelResponse.Cart;
using BusinessLogicLayer.ModelResponse.Order;
using BusinessLogicLayer.ModelResponse.Product;

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
            CartProfile();
            ProductProfile();
            ProductCategoryProfile();
            OrderProfile();
            NotificationProfile();
            FollowProfile();
            AddressProfile();
            DecorServiceProfile();
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
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId));

            CreateMap<CreateAccountRequest, Account>();
            CreateMap<UpdateAccountRequest, Account>();
        }

        private void DecorCategoryProfile()
        {
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
            CreateMap<Provider, ProviderResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.Bio))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.IsProvider, opt => opt.MapFrom(src => src.IsProvider))
                .ForMember(dest => dest.JoinedDate, opt => opt.MapFrom(src => src.JoinedDate))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Account.Phone));
                
            // FollowersCount và FollowingsCount sẽ gán trong service (chứ không map DB).

            CreateMap<BecomeProviderRequest, Provider>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.Bio))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.JoinedDate, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.IsProvider, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.Account, opt => opt.MapFrom(src => new Account
                {
                    Phone = src.Phone
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

        private void CartProfile()
        {
            CreateMap<Cart, CartResponse>()
            .ForMember(dest => dest.TotalItem, opt => opt.MapFrom(src => src.TotalItem))
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
            .ForMember(dest => dest.CartItems, opt => opt.MapFrom(src => src.CartItems));

            CreateMap<CartItem, CartItemResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CartId, opt => opt.MapFrom(src => src.CartId))
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
                .ForMember(dest => dest.Image, opt => opt.MapFrom(src => src.Image))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice));
        }

        private void ProductProfile()
        {
            CreateMap<ProductRequest, Product>();
            CreateMap<Product, ProductResponse>();
        }

        private void ProductCategoryProfile()
        {
            CreateMap<ProductCategoryRequest, ProductCategory>();
            CreateMap<ProductCategory, ProductCategoryResponse>();
        }

        private void OrderProfile()
        {
            CreateMap<OrderRequest, Order>();
            CreateMap<Order, OrderResponse>();
        }

        private void NotificationProfile()
        {
            CreateMap<Notification, NotificationResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.NotifiedAt, opt => opt.MapFrom(src => src.NotifiedAt))
                // Dù vẫn map ReceiverId (AccountId) nếu cần, nhưng không map tên người nhận
                .ForMember(dest => dest.ReceiverId, opt => opt.MapFrom(src => src.AccountId))
                // Map SenderId và SenderName từ đối tượng Sender
                .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.SenderId))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src =>
                    src.Sender != null
                        ? $"{src.Sender.FirstName} {src.Sender.LastName}"
                        : "System"))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
        }

        private void FollowProfile()
        {
            // Map cho trường hợp GetFollowers: hiển thị thông tin của người theo dõi (follower)
            CreateMap<Follow, FollowerResponse>()
                .ForMember(dest => dest.FollowerId,
                           opt => opt.MapFrom(src => src.FollowerId))
                .ForMember(dest => dest.FollowerName,
                           opt => opt.MapFrom(src => ((src.Follower.FirstName ?? "") + " " + (src.Follower.LastName ?? "")).Trim()))
                .ForMember(dest => dest.FollowerAvatar,
                           opt => opt.MapFrom(src => src.Follower.Avatar))
                .ForMember(dest => dest.FollowedAt,
                           opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));

            // Map cho trường hợp GetFollowings: hiển thị thông tin của người được theo dõi (following)
            CreateMap<Follow, FollowingResponse>()
                .ForMember(dest => dest.FollowingId,
                           opt => opt.MapFrom(src => src.FollowingId))
                .ForMember(dest => dest.FollowingName,
                           opt => opt.MapFrom(src => ((src.Following.FirstName ?? "") + " " + (src.Following.LastName ?? "")).Trim()))
                .ForMember(dest => dest.FollowingAvatar,
                           opt => opt.MapFrom(src => src.Following.Avatar))
                .ForMember(dest => dest.FollowedAt,
                           opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        private void AddressProfile() 
        {
            CreateMap<Address, AddressResponse>()
                .ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => src.Type.ToString()));
        }

        private void DecorServiceProfile() 
        {
            CreateMap<DecorService, DecorServiceDTO>();
        }
    }
}
