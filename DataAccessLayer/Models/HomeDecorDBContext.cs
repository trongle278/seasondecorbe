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
        public DbSet<Follower> Followers { get; set; }
        public DbSet<FollowerActivity> FollowerActivities { get; set; }
        public DbSet<Decorator> Decorators { get; set; }
        public DbSet<DecorService> DecorServices { get; set; }
        public DbSet<DecorImage> DecorImages { get; set; }
        public DbSet<DecorCategory> DecorCategories { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<ServicePromote> ServicePromotes { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Support> Supports { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<TicketReply> TicketReplies { get; set; }
        public DbSet<Chat> Chats { get; set; }

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

            // Configure N-N relationship between Account and FollowerActivity
            modelBuilder.Entity<Follower>()
                .HasKey(f => new { f.AccountId, f.FollowerId });

            modelBuilder.Entity<Follower>()
                .HasOne(f => f.Account)
                .WithMany(a => a.Followers)
                .HasForeignKey(f => f.AccountId);

            modelBuilder.Entity<Follower>()
                .HasOne(f => f.FollowerActivity)
                .WithMany(fa => fa.Followers)
                .HasForeignKey(f => f.FollowerId);

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

            // Configure 1-1 relationship between Account and Decorator
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Decorator)
                .WithOne(d => d.Account)
                .HasForeignKey<Decorator>(d => d.AccountId);

            // Configure 1-1 relationship between Booking and DecorService
            modelBuilder.Entity<DecorService>()
                .HasOne(ds => ds.Booking)
                .WithOne(b => b.DecorService)
                .HasForeignKey<Booking>(b => b.DecorServiceId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationsip between Decorator and DecorService
            modelBuilder.Entity<DecorService>()
                .HasOne(ds => ds.Decorator)
                .WithMany(d => d.DecorServices)
                .HasForeignKey(ds => ds.DecoratorId);

            // Configure N-N relationship between Promotion and DecorService
            modelBuilder.Entity<ServicePromote>()
                .HasKey(sp => new { sp.DecorServiceId, sp.PromotionId });

            modelBuilder.Entity<ServicePromote>()
                .HasOne(sp => sp.DecorService)
                .WithMany(ds => ds.ServicePromotes)
                .HasForeignKey(sp => sp.DecorServiceId);

            modelBuilder.Entity<ServicePromote>()
                .HasOne(sp => sp.Promotion)
                .WithMany(p => p.ServicePromotes)
                .HasForeignKey(sp => sp.PromotionId);

            // Configure N-N relationship between Booking and TimeSlot
            modelBuilder.Entity<BookingDetail>()
                .HasKey(bd => new { bd.BookingId, bd.TimeSlotId });

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(bd => bd.BookingId);

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.TimeSlot)
                .WithMany(ts => ts.BookingDetails)
                .HasForeignKey(bd => bd.TimeSlotId);

            // Configure 1-N relationship between Voucher and Booking
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Voucher)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VoucherId);

            // Configure 1-1 relationship between Booking and Review
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Review)
                .WithOne(r => r.Booking)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-1 relationship between Payment and Booking
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
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
        }
    }
}
