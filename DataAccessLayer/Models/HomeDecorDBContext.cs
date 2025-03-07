using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccessObject.Models
{
    public class HomeDecorDBContext : DbContext
    {
        public HomeDecorDBContext() { }

        public HomeDecorDBContext(DbContextOptions<HomeDecorDBContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<DecorService> DecorServices { get; set; }
        public DbSet<DecorImage> DecorImages { get; set; }
        public DbSet<DecorCategory> DecorCategories { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<ProductOrder> ProductOrders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Support> Supports { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<TicketReply> TicketReplies { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatFile> ChatFiles { get; set; }
        //test
        public DbSet<DeviceToken> DeviceTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure 1-N relationship between Role and Account
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Role)
                .WithMany(r => r.Accounts)
                .HasForeignKey(a => a.RoleId);

            // Configure 1-N relationship between Account and Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Account)
                .WithMany(a => a.Notifications)
                .HasForeignKey(n => n.AccountId);

            // Configure 1-N relationship between TicketType and Support
            modelBuilder.Entity<Support>()
                .HasOne(s => s.TicketType)
                .WithMany(tt => tt.Supports)
                .HasForeignKey(s => s.TicketTypeId);

            // Configure 1-N relationsip between DecorCategory and DecorService
            modelBuilder.Entity<DecorService>()
                .HasOne(ds => ds.DecorCategory)
                .WithMany(dc => dc.DecorServices)
                .HasForeignKey(ds => ds.DecorCategoryId);

            // Configure 1-N relationsip between DecorService and DecorImage
            modelBuilder.Entity<DecorImage>()
                .HasOne(di => di.DecorService)
                .WithMany(ds => ds.DecorImages)
                .HasForeignKey(di => di.DecorServiceId);

            // Configure 1-N relationship between Account and Support
            modelBuilder.Entity<Support>()
                .HasOne(s => s.Account)
                .WithMany(a => a.Supports)
                .HasForeignKey(s => s.AccountId);

            // Configure 1-N relationship between Support and TicketReply
            modelBuilder.Entity<TicketReply>()
                .HasOne(tr => tr.Support)
                .WithMany(s => s.TicketReplies)
                .HasForeignKey(tr => tr.SupportId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Account and TicketReply
            modelBuilder.Entity<TicketReply>()
                .HasOne(tr => tr.Account)
                .WithMany(a => a.TicketReplies)
                .HasForeignKey(tr => tr.AccountId);

            // Configure 1-N relationship between Support and TicketAttachment
            modelBuilder.Entity<TicketAttachment>()
                .HasOne(ta => ta.Support)
                .WithMany(s => s.TicketAttachments)
                .HasForeignKey(ta => ta.SupportId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between TicketReply and TicketAttachment
            modelBuilder.Entity<TicketAttachment>()
                .HasOne(ta => ta.TicketReply)
                .WithMany(tr => tr.TicketAttachments)
                .HasForeignKey(ta => ta.TicketReplyId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Account and Booking
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Account)
                .WithMany(a => a.Bookings)
                .HasForeignKey(b => b.AccountId);

            // Configure 1-N relationship between Account and Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Account)
                .WithMany(a => a.Payments)
                .HasForeignKey(p => p.AccountId);

            // Configure 1-N relationship between Account and Review
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Account)
                .WithMany(a => a.Reviews)
                .HasForeignKey(r => r.AccountId);

            // Configure 1-1 relationship between Account and Provider
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Provider)
                .WithOne(d => d.Account)
                .HasForeignKey<Provider>(d => d.AccountId);

            // Configure 1-N relationship between Booking and DecorService
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.DecorService)
                .WithMany(ds => ds.Bookings)  // thay đổi từ WithOne sang WithMany
                .HasForeignKey(b => b.DecorServiceId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationsip between Account and DecorService
            modelBuilder.Entity<DecorService>()
                .HasOne(ds => ds.Account)
                .WithMany(a => a.DecorServices)
                .HasForeignKey(ds => ds.AccountId);

            // Configure 1-N relationship between Voucher and Booking
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Voucher)
                .WithMany(v => v.Bookings)
                .IsRequired(false)
                .HasForeignKey(b => b.VoucherId);
                

            // Configure 1-1 relationship between Booking and Review
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Review)
                .WithOne(r => r.Booking)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Booking and Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-1 relationship between User and Cart
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Cart)
                .WithOne(c => c.Account)
                .HasForeignKey<Cart>(c => c.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Product and Provider
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Provider)
                .WithMany(pr => pr.Products)
                .HasForeignKey(p => p.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between ProductImage and Product
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Cart and CartItem
            modelBuilder.Entity<Cart>()
                .HasMany(c => c.CartItems)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Product and CartItem
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Voucher and Cart
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Voucher)
                .WithMany(v => v.Carts)
                .HasForeignKey(c => c.VoucherId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between User and Order
            modelBuilder.Entity<Account>()
                .HasMany(a => a.Orders)
                .WithOne(o => o.Account)
                .HasForeignKey(o => o.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Order and ProductOrder
            modelBuilder.Entity<Order>()
                .HasMany(o => o.ProductOrders)
                .WithOne(po => po.Order)
                .HasForeignKey(po => po.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Product and ProductOrder
            modelBuilder.Entity<ProductOrder>()
                .HasOne(po => po.Product)
                .WithMany(p => p.ProductOrders)
                .HasForeignKey(po => po.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Order and Address
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Address)
                .WithMany(a => a.Orders)
                .HasForeignKey(o => o.AddressId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Order and Review
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Reviews)
                .WithOne(f => f.Order)
                .HasForeignKey(f => f.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Order and Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Subscription and Account
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Subscription)
                .WithMany(sb => sb.Accounts)
                .HasForeignKey(a => a.SubscriptionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Subscription and Provider
            modelBuilder.Entity<Provider>()
                .HasOne(p => p.Subscription)
                .WithMany(sb => sb.Providers)
                .HasForeignKey(a => a.SubscriptionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Receiver)
                .WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Configure 1-N relationship between Chat and ChatFile
            modelBuilder.Entity<ChatFile>()
                .HasOne(cf => cf.Chat)
                .WithMany(c => c.ChatFiles)
                .HasForeignKey(cf => cf.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            
            //sửa quan hệ follow
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(a => a.Followings)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Following)
                .WithMany(a => a.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            //test
            modelBuilder.Entity<DeviceToken>()
                .HasOne(dt => dt.Account)
                .WithMany(a => a.DeviceTokens)
                .HasForeignKey(dt => dt.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentPhase>()
                .HasOne(pp => pp.Booking)
                .WithMany(b => b.PaymentPhases)
                .HasForeignKey(pp => pp.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.PaymentPhase)
                .WithMany(pp => pp.Payments)
                .HasForeignKey(p => p.PaymentPhaseId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "Provider" },
                new Role { Id = 3, RoleName = "Customer" }
            );

            modelBuilder.Entity<Subscription>().HasData(
                new Subscription { Id = 1, Name = "Basic", Description = "Normal Package", Price = 0, Duration = 999 }
            );

            modelBuilder.Entity<ProductCategory>().HasData(
                new ProductCategory { Id = 1, CategoryName = "Lamp"},
                new ProductCategory { Id = 2, CategoryName = "Clock"},
                new ProductCategory { Id = 3, CategoryName = "Bed"},
                new ProductCategory { Id = 4, CategoryName = "Chest"},
                new ProductCategory { Id = 5, CategoryName = "Desk"},
                new ProductCategory { Id = 6, CategoryName = "Cabinet"},
                new ProductCategory { Id = 7, CategoryName = "Chair"},
                new ProductCategory { Id = 8, CategoryName = "Sofa"},
                new ProductCategory { Id = 9, CategoryName = "Bookshelf"},
                new ProductCategory { Id = 10, CategoryName = "Table"},
                new ProductCategory { Id = 11, CategoryName = "Couch"},
                new ProductCategory { Id = 12, CategoryName = "Hanger"},
                new ProductCategory { Id = 13, CategoryName = "Closet"},
                new ProductCategory { Id = 14, CategoryName = "Vanity"}
            );
        }
    }
}
