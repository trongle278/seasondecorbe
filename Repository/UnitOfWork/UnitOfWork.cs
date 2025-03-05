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
            ProviderRepository = new ProviderRepository(_context);
            ProductRepository = new ProductRepository(_context);
            ProductImageRepository = new ProductImageRepository(_context);
            ProductCategoryRepository = new ProductCategoryRepository(_context);
            CartRepository = new CartRepository(_context);
            CartItemRepository = new CartItemRepository(_context);
            OrderRepository = new OrderRepository(_context);
            ProductOrderRepository = new ProductOrderRepository(_context);
            TicketTypeRepository = new TicketTypeRepository(_context);
            SupportRepository = new SupportRepository(_context);
            NotificationRepository = new NotificationRepository(_context);
            FollowRepository = new FollowRepository(_context);
            DeviceTokenRepository = new DeviceTokenRepository(_context);
            DecorServiceRepository = new DecorServiceRepository(_context);
            AddressRepository = new AddressRepository(_context);
            ReviewRepository = new ReviewRepository(_context);
            BookingRepository = new BookingRepository(_context);
            PaymentPhaseRepository = new PaymentPhaseRepository(_context);
        }

        public IAccountRepository AccountRepository { get; private set; }
        public IProviderRepository ProviderRepository { get; private set; }
        public IRoleRepository RoleRepository { get; private set; }
        public IDecorCategoryRepository DecorCategoryRepository { get; private set; }
        public IChatRepository ChatRepository { get; private set; }
        public IProductRepository ProductRepository { get; private set; }
        public IProductImageRepository ProductImageRepository { get; private set; }
        public IProductCategoryRepository ProductCategoryRepository { get; private set; }
        public ICartRepository CartRepository { get; private set; }
        public ICartItemRepository CartItemRepository { get; private set; }
        public IOrderRepository OrderRepository { get; private set; }
        public IProductOrderRepository ProductOrderRepository { get; private set; }
        public ITicketTypeRepository TicketTypeRepository { get; private set; }
        public ISupportRepository SupportRepository { get; private set; }
        public INotificationRepository NotificationRepository { get; private set; }
        public IFollowRepository FollowRepository { get; private set; }
        public IDeviceTokenRepository DeviceTokenRepository { get; private set; }
        public IDecorServiceRepository DecorServiceRepository { get; private set; }
        public IAddressRepository AddressRepository { get; private set; }   
        public IReviewRepository ReviewRepository { get; private set; }
        public IBookingRepository BookingRepository { get; private set; }
        public IPaymentPhaseRepository PaymentPhaseRepository { get; private set; }
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
