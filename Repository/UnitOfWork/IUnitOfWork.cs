using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Repository.Interfaces;

namespace Repository.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository AccountRepository { get; }
        IRoleRepository RoleRepository { get; }
        IDecorCategoryRepository DecorCategoryRepository { get; }
        IChatRepository ChatRepository { get; }
        IProductRepository ProductRepository { get; }
        IProductImageRepository ProductImageRepository { get; }
        IProductCategoryRepository ProductCategoryRepository { get; }
        ICartRepository CartRepository { get; }
        ICartItemRepository CartItemRepository { get; }
        IOrderRepository OrderRepository { get; }
        IOrderDetailRepository OrderDetailRepository { get; }
        ITicketTypeRepository TicketTypeRepository { get; }
        ISupportRepository SupportRepository { get; }
        INotificationRepository NotificationRepository { get; }
        IFollowRepository FollowRepository { get; }
        IDecorServiceRepository DecorServiceRepository { get; }
        IAddressRepository AddressRepository { get; }
        IReviewRepository ReviewRepository { get; }
        IReviewImageRepository ReviewImageRepository { get; }
        IBookingRepository BookingRepository { get; }
        IWalletRepository WalletRepository { get; }
        IPaymentTransactionRepository PaymentTransactionRepository { get; }
        IWalletTransactionRepository WalletTransactionRepository { get; }
        IContactRepository ContactRepository { get; }
        IFavoriteServiceRepository FavoriteServiceRepository { get; }
        IFavoriteProductRepository FavoriteProductRepository { get; }
        ISeasonRepository SeasonRepository { get; }
        ISettingRepository SettingRepository { get; }
        IBookingDetailRepository BookingDetailRepository { get; }
        ITrackingRepository TrackingRepository { get; }
        IQuotationRepository QuotationRepository { get; }
        IMaterialDetailRepository MaterialDetailRepository { get; }
        ILaborDetailRepository LaborDetailRepository { get; }
        ITimeSlotRepository TimeSlotRepository { get; }
        IContractRepository ContractRepository { get; }
        ICancelTypeRepository CancelTypeRepository { get; }
        IProductDetailRepository ProductDetailRepository { get; }
        ITicketAttachmentRepository TicketAttachmentRepository { get; }
        ITicketReplyRepository TicketReplyRepository { get; }
        ITrackingImageRepository TrackingImageRepository { get; }
        IApplicationHistoryRepository ApplicationHistoryRepository { get; }
        ISkillRepository SkillRepository { get; }
        IDecorationStyleRepository DecorationStyleRepository { get; }
        ICertificateImageRepository CertificateImageRepository { get; }
        IZoomRepository ZoomRepository { get; }
        IThemeColorRepository ThemeColorRepository { get; }
        IOfferingRepository OfferingRepository { get; }
        IProductSeasonRepository ProductSeasonRepository { get; }
        IRelatedProductRepository RelatedProductRepository { get; }
        IRelatedProductItemRepository RelatedProductItemRepository { get; }
        int Save();
        Task CommitAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
