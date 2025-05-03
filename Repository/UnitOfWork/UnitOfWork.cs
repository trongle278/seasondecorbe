using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Repository.Interfaces;
using Repository.Repositories;

namespace Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HomeDecorDBContext _context;

        public UnitOfWork(HomeDecorDBContext context, IConfiguration configuration)
        {
            _context = context;
            AccountRepository = new AccountRepository(_context);
            RoleRepository = new RoleRepository(_context);
            DecorCategoryRepository = new DecorCategoryRepository(_context);
            ChatRepository = new ChatRepository(_context);
            ProductRepository = new ProductRepository(_context);
            ProductImageRepository = new ProductImageRepository(_context);
            ProductCategoryRepository = new ProductCategoryRepository(_context);
            CartRepository = new CartRepository(_context);
            CartItemRepository = new CartItemRepository(_context);
            OrderRepository = new OrderRepository(_context);
            OrderDetailRepository = new OrderDetailRepository(_context);
            TicketTypeRepository = new TicketTypeRepository(_context);
            SupportRepository = new SupportRepository(_context);
            NotificationRepository = new NotificationRepository(_context);
            FollowRepository = new FollowRepository(_context);
            DeviceTokenRepository = new DeviceTokenRepository(_context);
            DecorServiceRepository = new DecorServiceRepository(_context);
            AddressRepository = new AddressRepository(_context);
            ReviewRepository = new ReviewRepository(_context);
            ReviewImageRepository = new ReviewImageRepository(_context);
            BookingRepository = new BookingRepository(_context);
            WalletRepository = new WalletRepository(_context);
            WalletTransactionRepository = new WalletTransactionRepository(_context);
            PaymentTransactionRepository = new PaymentTransactionRepository(_context);
            SettingRepository = new SettingRepository(_context);
            ContactRepository = new ContactRepository(_context);
            FavoriteServiceRepository = new FavoriteServiceRepository(_context);
            FavoriteProductRepository = new FavoriteProductRepository(_context);
            SeasonRepository = new SeasonRepository(_context);
            BookingDetailRepository = new BookingDetailRepository(_context);
            TrackingRepository = new TrackingRepository(_context);
            QuotationRepository = new QuotationRepository(_context);
            MaterialDetailRepository = new MaterialDetailRepository(_context);
            LaborDetailRepository = new LaborDetailRepository(_context);
            TimeSlotRepository = new TimeSlotRepository(_context);
            ContractRepository = new ContractRepository(_context);
            SubscriptionRepository = new SubscriptionRepository(_context);
            VoucherRepository = new VoucherRepository(_context);
            CancelTypeRepository = new CancelTypeRepository(_context);
            ProductDetailRepository = new ProductDetailRepository(_context);
            TicketAttachmentRepository = new TicketAttachmentRepository(_context);
            TicketReplyRepository = new TicketReplyRepository(_context);
            TrackingImageRepository = new TrackingImageRepository(_context);
            ApplicationHistoryRepository = new ApplicationHistoryRepository(_context);
            SkillRepository = new SkillRepository(_context);
            DecorationStyleRepository = new DecorationStyleRepository(_context);
            CertificateImageRepository = new CertificateImageRepository(_context);
        }

        public IAccountRepository AccountRepository { get; private set; }
        public IRoleRepository RoleRepository { get; private set; }
        public IDecorCategoryRepository DecorCategoryRepository { get; private set; }
        public IChatRepository ChatRepository { get; private set; }
        public IProductRepository ProductRepository { get; private set; }
        public IProductImageRepository ProductImageRepository { get; private set; }
        public IProductCategoryRepository ProductCategoryRepository { get; private set; }
        public ICartRepository CartRepository { get; private set; }
        public ICartItemRepository CartItemRepository { get; private set; }
        public IOrderRepository OrderRepository { get; private set; }
        public IOrderDetailRepository OrderDetailRepository { get; private set; }
        public ITicketTypeRepository TicketTypeRepository { get; private set; }
        public ISupportRepository SupportRepository { get; private set; }
        public INotificationRepository NotificationRepository { get; private set; }
        public IFollowRepository FollowRepository { get; private set; }
        public IDeviceTokenRepository DeviceTokenRepository { get; private set; }
        public IDecorServiceRepository DecorServiceRepository { get; private set; }
        public IAddressRepository AddressRepository { get; private set; }   
        public IReviewRepository ReviewRepository { get; private set; }
        public IReviewImageRepository ReviewImageRepository { get; private set; }
        public IBookingRepository BookingRepository { get; private set; }
        public IWalletRepository WalletRepository { get; private set; }
        public IPaymentTransactionRepository PaymentTransactionRepository { get; }
        public IWalletTransactionRepository WalletTransactionRepository { get; }
        public IContactRepository ContactRepository { get; private set; }
        public IFavoriteServiceRepository FavoriteServiceRepository { get; private set; }
        public IFavoriteProductRepository FavoriteProductRepository { get; private set; }
        public ISeasonRepository SeasonRepository { get; private set; }
        public ISettingRepository SettingRepository { get; private set; }
        public IBookingDetailRepository BookingDetailRepository { get; private set; }
        public ITrackingRepository TrackingRepository { get; private set; }
        public IQuotationRepository QuotationRepository { get; private set; }
        public IMaterialDetailRepository MaterialDetailRepository { get; private set; }
        public ILaborDetailRepository LaborDetailRepository { get; private set; }
        public ITimeSlotRepository TimeSlotRepository { get; private set; }
        public IContractRepository ContractRepository { get; private set; }
        public ISubscriptionRepository SubscriptionRepository { get; private set; }
        public IVoucherRepository VoucherRepository { get; private set; }
        public ICancelTypeRepository CancelTypeRepository { get; private set; }
        public IProductDetailRepository ProductDetailRepository { get; private set; }
        public ITicketAttachmentRepository TicketAttachmentRepository { get; private set; }
        public ITicketReplyRepository TicketReplyRepository { get; private set; }
        public ITrackingImageRepository TrackingImageRepository { get; private set; }
        public IApplicationHistoryRepository ApplicationHistoryRepository { get; private set; }
        public ISkillRepository SkillRepository { get; private set; }
        public IDecorationStyleRepository DecorationStyleRepository { get; private set; }
        public ICertificateImageRepository CertificateImageRepository { get; private set; }
        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task CommitAsync()
            => await _context.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();

        public int Save()
        {
            return _context.SaveChanges();
        }
    }
}
