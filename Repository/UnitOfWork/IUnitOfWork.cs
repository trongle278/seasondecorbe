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
        IProductOrderRepository ProductOrderRepository { get; }
        ITicketTypeRepository TicketTypeRepository { get; }
        ISupportRepository SupportRepository { get; }
        INotificationRepository NotificationRepository { get; }
        IFollowRepository FollowRepository { get; }
        IDeviceTokenRepository DeviceTokenRepository { get; }
        IDecorServiceRepository DecorServiceRepository { get; }
        IAddressRepository AddressRepository { get; }
        IReviewRepository ReviewRepository { get; }
        IBookingRepository BookingRepository { get; }
        IWalletRepository WalletRepository { get; }
        IPaymentTractionRepository PaymentTractionRepository { get; }
        IWalletTransactionRepository WalletTransactionRepository { get; }
        IContactRepository ContactRepository { get; }
        IFavoriteServiceRepository FavoriteServiceRepository { get; }
        ISeasonRepository SeasonRepository { get; }
        ISettingRepository SettingRepository { get; }
        IBookingDetailRepository BookingDetailRepository { get; }
        ITrackingRepository TrackingRepository { get; }
        int Save();
        Task CommitAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
