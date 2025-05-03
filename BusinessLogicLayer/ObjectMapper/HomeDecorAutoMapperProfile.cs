using System;
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
using BusinessLogicLayer.ModelResponse.Payment;
using BusinessLogicLayer.ModelRequest.Review;
using BusinessLogicLayer.ModelResponse.Review;
using static BusinessLogicLayer.ModelResponse.DecorServiceReviewResponse;
using BusinessLogicLayer.Interfaces;

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
            FollowProfile();
            AddressProfile();
            DecorServiceProfile();
            WalletProfile();
            ReviewProfile();
            SettingProfile();
        }

        private void AccountProfile()
        {
            CreateMap<Account, AccountDTO>()
                .ForMember(dest => dest.IsProvider, opt => opt.MapFrom(src => src.IsProvider))
                .ForMember(dest => dest.ProviderVerified, opt => opt.MapFrom(src => src.ProviderVerified))
                .ForMember(dest => dest.IsDisable, opt => opt.MapFrom(src => src.IsDisable));
            CreateMap<CreateAccountRequest, Account>();
            CreateMap<UpdateAccountRequest, Account>();
        }

        private void DecorCategoryProfile()
        {
            CreateMap<DecorCategory, DecorCategoryDTO>();

            CreateMap<DecorCategoryRequest, DecorCategory>();
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
            CreateMap<Account, ProviderResponse>()
                .ForMember(dest => dest.BusinessName, opt => opt.MapFrom(src => src.BusinessName))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.BusinessAddress))
                .ForMember(dest => dest.ProviderStatus, opt => opt.MapFrom(src => src.ProviderStatus))
                
                .ForMember(dest => dest.SkillName, opt => opt.MapFrom(src => src.Skill.Name))
                .ForMember(dest => dest.DecorationStyleName, opt => opt.MapFrom(src => src.DecorationStyle.Name))
                .ForMember(dest => dest.YearsOfExperience, opt => opt.MapFrom(src => src.YearsOfExperience))
                .ForMember(dest => dest.PastWorkPlaces, opt => opt.MapFrom(src => src.PastWorkPlaces))
                .ForMember(dest => dest.PastProjects, opt => opt.MapFrom(src => src.PastProjects))
                .ForMember(dest => dest.CertificateImageUrls, opt =>
                                    opt.MapFrom(src => src.CertificateImages.Select(ci => ci.ImageUrl)));

        // FollowersCount và FollowingsCount sẽ gán trong service (chứ không map DB).

        CreateMap<BecomeProviderRequest, Account>()
                .ForMember(dest => dest.JoinedDate, opt => opt.MapFrom(src => DateTime.UtcNow.ToLocalTime()))
                .ForMember(dest => dest.IsProvider, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => 1));
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
                //.ForMember(dest => dest.TicketStatus, opt => opt.MapFrom(src => (int)src.TicketStatus))
                .ForMember(dest => dest.TicketType, opt => opt.MapFrom(src => src.TicketType.Type))
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
            CreateMap<CreateProductRequest, Product>();
            CreateMap<Product, CreateProductResponse>();

            CreateMap<UpdateProductRequest, Product>();
            CreateMap<Product, UpdateProductResponse>();

            CreateMap<ProductListRequest, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(src =>
                    src.ImageUrls != null ? src.ImageUrls.Select(url => new ProductImage { ImageUrl = url }).ToList()
                                          : new List<ProductImage>()));
            CreateMap<Product, ProductListResponse>()
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src =>
                    src.ProductImages != null ? src.ProductImages.Select(pi => pi.ImageUrl).ToList()
                                              : new List<string>()));

            CreateMap<ProductDetailRequest, Product>()
                .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(src =>
                    src.ImageUrls != null ? src.ImageUrls.Select(url => new ProductImage { ImageUrl = url }).ToList()
                                          : new List<ProductImage>()));
            CreateMap<Product, ProductDetailResponse>()
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src =>
                    src.ProductImages != null ? src.ProductImages.Select(pi => pi.ImageUrl).ToList()
                                              : new List<string>()));

            CreateMap<Product, RelatedProductResponse>()
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src =>
                    src.ProductImages != null ? src.ProductImages.Select(pi => pi.ImageUrl).ToList()
                                              : new List<string>()));
        }

        private void ProductCategoryProfile()
        {
            CreateMap<ProductCategoryRequest, ProductCategory>();
            CreateMap<ProductCategory, ProductCategoryResponse>();
        }

        private void OrderProfile()
        {
            CreateMap<OrderRequest, Order>();
            CreateMap<Order, OrderResponse>()
                .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails));

            CreateMap<OrderDetailRequest, OrderDetail>();
            CreateMap<OrderDetail, OrderDetailResponse>()
                .ForMember(dest => dest.Provider, opt => opt.MapFrom(src => src.Product.Account));

            CreateMap<Account, OrderProviderResponse>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.BusinessAddress));
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

            CreateMap<Address, OrderAddressResponse>()
                .ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => src.Type.ToString()));
        }

        private void DecorServiceProfile()
        {
            CreateMap<DecorService, DecorServiceDTO>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));
            //.ForMember(dest => dest.IsBooked, opt => opt.MapFrom(src => src.Account.IsBooked));
            CreateMap<DecorImage, DecorImageResponse>();


            CreateMap<DecorService, DecorServiceById>();

            CreateMap<ReviewImage, DecorServiceReviewResponse.DecorServiceReviewImageResponse>()
    .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl));

            CreateMap<Review, DecorServiceReviewResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Account.LastName + " " + src.Account.FirstName))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Account.Avatar))
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreateAt.ToString("dd/MM/yyyy")));

        }

        private void WalletProfile()
        {
            CreateMap<WalletTransaction, TransactionsResponse>();
        }

        private void ReviewProfile()
        {
            CreateMap<ReviewOrderRequest, Review>();
            CreateMap<Product, ProductReviewResponse>();

            CreateMap<UpdateOrderReviewRequest, Product>();
            CreateMap<Product, UpdateProductReviewResponse>();

            CreateMap<ReviewBookingRequest, Review>();
            CreateMap<Product, ServiceReviewResponse>();

            CreateMap<UpdateBookingReviewRequest, Product>();
            CreateMap<Product, UpdateServiceReviewResponse>();

            CreateMap<ReviewRequest, Review>();
            CreateMap<Review, ReviewResponse>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src =>
                    src.ReviewImages != null ? src.ReviewImages.Select(pi => pi.ImageUrl).ToList()
                                              : new List<string>()))
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src =>
                    src.IsUpdated ? src.UpdateAt : src.CreateAt))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src =>
                    src.Account.LastName + " " + src.Account.FirstName))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src =>
                    src.Account.Avatar));
        }

        private void SettingProfile()
        {
            CreateMap<SettingRequest, Setting>();
            CreateMap<Setting, SettingResponse>();
        }
    }
}
