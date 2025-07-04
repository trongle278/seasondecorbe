﻿using System;
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
        public DbSet<DecorService> DecorServices { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<DecorServiceSeason> DecorServiceSeasons { get; set; }
        public DbSet<DecorImage> DecorImages { get; set; }
        public DbSet<DecorCategory> DecorCategories { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Support> Supports { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<TicketReply> TicketReplies { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatFile> ChatFiles { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<FavoriteService> FavoriteServices { get; set; }
        public DbSet<FavoriteProduct> FavoriteProducts { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<Tracking> Trackings { get; set; }
        public DbSet<MaterialDetail> MaterialDetails { get; set; }
        public DbSet<LaborDetail> LaborDetails { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<CancelType> CancelTypes { get; set; }
        public DbSet<TrackingImage> TrackingImages { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ProductDetail> ProductDetails { get; set; }
        public DbSet<ApplicationHistory> ApplicationHistories { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<DecorationStyle> DecorationStyles { get; set; }
        public DbSet<ZoomMeeting> ZoomMeetings { get; set; }
        public DbSet<ProductSeason> ProductSeasons { get; set; }
        public DbSet<RelatedProduct> RelatedProducts { get; set; }
        public DbSet<RelatedProductItem> RelatedProductItems { get; set; }
        public DbSet<BookingForm> BookingForms { get; set; }
        public DbSet<ScopeOfWork> ScopeOfWorks { get; set; }
        public DbSet<ScopeOfWorkForm> ScopeOfWorkForms { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Quotation>()
                .Property(q => q.QuotationFilePath)
                .IsRequired(false);

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

            // Configure Many-to-Many between DecorService and Season
            modelBuilder.Entity<DecorServiceSeason>()
                .HasOne(dss => dss.DecorService)
                .WithMany(ds => ds.DecorServiceSeasons)
                .HasForeignKey(dss => dss.DecorServiceId);

            modelBuilder.Entity<DecorServiceSeason>()
                .HasOne(dss => dss.Season)
                .WithMany(s => s.DecorServiceSeasons)
                .HasForeignKey(dss => dss.SeasonId);

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
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Configure 1-N relationship between TicketReply and TicketAttachment
            modelBuilder.Entity<TicketAttachment>()
                .HasOne(ta => ta.TicketReply)
                .WithMany(tr => tr.TicketAttachments)
                .HasForeignKey(ta => ta.TicketReplyId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Configure 1-N relationship between Account and Booking
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Account)
                .WithMany(a => a.Bookings)
                .HasForeignKey(b => b.AccountId);

            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(bd => bd.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Account and Review
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Account)
                .WithMany(a => a.Reviews)
                .HasForeignKey(r => r.AccountId);

            // Configure 1-1 relationship between Account and Wallet
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Wallet)
                .WithOne(d => d.Account)
                .HasForeignKey<Wallet>(d => d.AccountId);

            // Configure 1-N relationship between Booking and DecorService
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.DecorService)
                .WithMany(ds => ds.Bookings)
                .HasForeignKey(b => b.DecorServiceId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationsip between Account and DecorService
            modelBuilder.Entity<DecorService>()
                .HasOne(ds => ds.Account)
                .WithMany(a => a.DecorServices)
                .HasForeignKey(ds => ds.AccountId);

            // Configure 1-1 relationship between Booking and Review
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Review)
                .WithOne(r => r.Booking)
                .HasForeignKey<Review>(r => r.BookingId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between DecorService and Review
            modelBuilder.Entity<Review>()
                .HasOne(r => r.DecorService)
                .WithMany(ds => ds.Reviews)
                .HasForeignKey(r => r.ServiceId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-1 relationship between User and Cart
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Cart)
                .WithOne(c => c.Account)
                .HasForeignKey<Cart>(c => c.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Product and Account
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Account)
                .WithMany(pr => pr.Products)
                .HasForeignKey(p => p.AccountId)
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

            // Configure 1-N relationship between Product and ProductDetail
            modelBuilder.Entity<ProductDetail>()
                .HasOne(pd => pd.Product)
                .WithMany(p => p.ProductDetails)
                .HasForeignKey(pd => pd.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between User and Order
            modelBuilder.Entity<Account>()
                .HasMany(a => a.Orders)
                .WithOne(o => o.Account)
                .HasForeignKey(o => o.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Order and ProductOrder
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderDetails)
                .WithOne(po => po.Order)
                .HasForeignKey(po => po.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-N relationship between Product and ProductOrder
            modelBuilder.Entity<OrderDetail>()
                .HasOne(po => po.Product)
                .WithMany(p => p.OrderDetails)
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
                .WithOne(r => r.Order)
                .HasForeignKey(r => r.OrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Product and Review
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Reviews)
                .WithOne(r => r.Product)
                .HasForeignKey(r => r.ProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure 1-N relationship between Review and ReviewImage
            modelBuilder.Entity<Review>()
                .HasMany(r => r.ReviewImages)
                .WithOne(ri => ri.Review)
                .HasForeignKey(ri => ri.ReviewId)
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

            modelBuilder.Entity<Contact>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contact>()
                .HasOne(c => c.ContactUser)
                .WithMany()
                .HasForeignKey(c => c.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FavoriteService>()
                .HasOne(f => f.DecorService)
                .WithMany(d => d.FavoriteServices)
                .HasForeignKey(f => f.DecorServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavoriteService>()
                .HasOne(f => f.Account)
                .WithMany(a => a.FavoriteServices)
                .HasForeignKey(f => f.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FavoriteProduct>()
                .HasOne(f => f.Product)
                .WithMany(d => d.FavoriteProducts)
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavoriteProduct>()
                .HasOne(f => f.Account)
                .WithMany(a => a.FavoriteProducts)
                .HasForeignKey(f => f.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Address)
                .WithMany(a => a.Bookings)
                .HasForeignKey(b => b.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tracking>()
                .HasOne(t => t.Booking)
                .WithMany(b => b.Trackings)
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaterialDetail>()
                .HasOne(md => md.Quotation)
                .WithMany(q => q.MaterialDetails)
                .HasForeignKey(md => md.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LaborDetail>()
                .HasOne(cd => cd.Quotation)
                .WithMany(q => q.LaborDetails)
                .HasForeignKey(cd => cd.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductDetail>()
                .HasOne(pd => pd.Quotation)
                .WithMany(q => q.ProductDetails)
                .HasForeignKey(pd => pd.QuotationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Support>()
                .HasOne(s => s.Booking)
                .WithMany(b => b.Supports)
                .HasForeignKey(s => s.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Quotation>()
                .HasOne(q => q.Booking)
                .WithMany(b => b.Quotations)
                .HasForeignKey(q => q.BookingId);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Quotation)
                .WithOne(q => q.Contract)
                .HasForeignKey<Contract>(c => c.QuotationId);

            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Account>()
                .HasOne(a => a.Skill)
                .WithMany(s => s.Accounts)
                .HasForeignKey(a => a.SkillId)
                .OnDelete(DeleteBehavior.Cascade); // hoặc .SetNull nếu muốn

            //modelBuilder.Entity<Account>()
            //    .HasOne(a => a.DecorationStyle)
            //    .WithMany()
            //    .HasForeignKey(a => a.DecorationStyleId)
            //    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ZoomMeeting>()
                .HasOne(z => z.Account)
                .WithMany(a => a.ZoomMeetings)
                .HasForeignKey(z => z.AccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ZoomMeeting>()
                .HasOne(z => z.Booking)
                .WithMany(b => b.ZoomMeetings)
                .HasForeignKey(z => z.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DecorServiceOffering>()
                .HasKey(dso => new { dso.DecorServiceId, dso.OfferingId });

            modelBuilder.Entity<DecorServiceOffering>()
                .HasOne(dso => dso.Offering)
                .WithMany(o => o.DecorServiceOfferings)
                .HasForeignKey(dso => dso.OfferingId);

            modelBuilder.Entity<DecorServiceOffering>()
                .HasOne(dso => dso.DecorService)
                .WithMany(ds => ds.DecorServiceOfferings)
                .HasForeignKey(dso => dso.DecorServiceId);

            modelBuilder.Entity<ProductSeason>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSeasons)
                .HasForeignKey(ps => ps.ProductId);

            modelBuilder.Entity<ProductSeason>()
                .HasOne(ps => ps.Season)
                .WithMany(s => s.ProductSeasons)
                .HasForeignKey(ps => ps.SeasonId);

            modelBuilder.Entity<RelatedProduct>()
                .HasOne(rp => rp.Booking)
                .WithOne(b => b.RelatedProduct)
                .HasForeignKey<Booking>(b => b.RelatedProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RelatedProduct>()
                .HasOne(rp => rp.DecorService)
                .WithMany(ds => ds.RelatedProducts)
                .HasForeignKey(rp => rp.ServiceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RelatedProduct>()
                .HasOne(rp => rp.Account)
                .WithMany(a => a.RelatedProducts)
                .HasForeignKey(rp => rp.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RelatedProductItem>()
                .HasOne(ri => ri.RelatedProduct)
                .WithMany(rp => rp.RelatedProductItems)
                .HasForeignKey(ri => ri.RelatedProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RelatedProductItem>()
                .HasOne(ri => ri.Product)
                .WithMany(p => p.RelatedProductItems)
                .HasForeignKey(ri => ri.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductDetail>()
                .HasOne(pd => pd.Booking)
                .WithMany(b => b.ProductDetails)
                .HasForeignKey(pd => pd.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.BookingForm)
                .WithOne(bf => bf.Booking)
                .HasForeignKey<Booking>(b => b.BookingFormId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingForm>()
                .HasOne(bf => bf.Account)
                .WithMany(a => a.BookingForms)
                .HasForeignKey(bf => bf.AccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FormImage>()
                .HasOne(fi => fi.BookingForm)
                .WithMany(bf => bf.FormImages)
                .HasForeignKey(fi => fi.BookingFormId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ScopeOfWorkForm>()
                .HasOne(sowf => sowf.BookingForm)
                .WithMany(bf => bf.ScopeOfWorkForms)
                .HasForeignKey(sowf => sowf.BookingFormId);

            modelBuilder.Entity<ScopeOfWorkForm>()
                .HasOne(sowf => sowf.ScopeOfWork)
                .WithMany(sow => sow.ScopeOfWorkForms)
                .HasForeignKey(sowf => sowf.ScopeOfWorkId);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "Provider" },
                new Role { Id = 3, RoleName = "Customer" }
            );
        
            modelBuilder.Entity<ProductCategory>().HasData(
                new ProductCategory { Id = 1, CategoryName = "Lamp" },
                new ProductCategory { Id = 2, CategoryName = "Clock" },
                new ProductCategory { Id = 3, CategoryName = "Bed" },
                new ProductCategory { Id = 4, CategoryName = "Chest" },
                new ProductCategory { Id = 5, CategoryName = "Desk" },
                new ProductCategory { Id = 6, CategoryName = "Cabinet" },
                new ProductCategory { Id = 7, CategoryName = "Chair" },
                new ProductCategory { Id = 8, CategoryName = "Sofa" },
                new ProductCategory { Id = 9, CategoryName = "Bookshelf" },
                new ProductCategory { Id = 10, CategoryName = "Table" },
                new ProductCategory { Id = 11, CategoryName = "Couch" },
                new ProductCategory { Id = 12, CategoryName = "Hanger" },
                new ProductCategory { Id = 13, CategoryName = "Closet" },
                new ProductCategory { Id = 14, CategoryName = "Vanity" }
            );

            modelBuilder.Entity<DecorCategory>().HasData(
                new DecorCategory { Id = 1, CategoryName = "Living Room" },
                new DecorCategory { Id = 2, CategoryName = "Bedroom" },
                new DecorCategory { Id = 3, CategoryName = "Kitchen" },
                new DecorCategory { Id = 4, CategoryName = "Bathroom" },
                new DecorCategory { Id = 5, CategoryName = "Home Office" },
                new DecorCategory { Id = 6, CategoryName = "Balcony & Garden" },
                new DecorCategory { Id = 8, CategoryName = "Dining Room" },
                new DecorCategory { Id = 9, CategoryName = "Entertainment Room" }
            );

            modelBuilder.Entity<Season>().HasData(
                new Season { Id = 1, SeasonName = "Spring" },
                new Season { Id = 2, SeasonName = "Summer" },
                new Season { Id = 3, SeasonName = "Autumn" },
                new Season { Id = 4, SeasonName = "Winter" },
                new Season { Id = 5, SeasonName = "Christmas" },
                new Season { Id = 6, SeasonName = "Tet" },
                new Season { Id = 7, SeasonName = "Valentine" },
                new Season { Id = 8, SeasonName = "Halloween" },
                new Season { Id = 9, SeasonName = "Easter" },
                new Season { Id = 10, SeasonName = "Birthday" },
                new Season { Id = 11, SeasonName = "Wedding" },
                new Season { Id = 12, SeasonName = "Anniversary" }
            );

            modelBuilder.Entity<Setting>().HasData(
                new Setting { Id = 1, Commission = (decimal)0.4 }
            );

            modelBuilder.Entity<CancelType>().HasData(
                new CancelType { Id = 1, Type = "I have changed my mind" },
                new CancelType { Id = 2, Type = "I found a better option" },
                new CancelType { Id = 3, Type = "My schedule conflicted" },
                new CancelType { Id = 4, Type = "An unexpected event occurred" },
                new CancelType { Id = 5, Type = "The address was incorrect" },
                new CancelType { Id = 6, Type = "I want to change my request" },
                new CancelType { Id = 7, Type = "Other reason" }
            );

            modelBuilder.Entity<Skill>().HasData(
                new Skill { Id = 1, Name = "Lighting Decoration" },
                new Skill { Id = 2, Name = "Space Decoration" },
                new Skill { Id = 3, Name = "Art Decoration" },
                new Skill { Id = 4, Name = "Event Decoration" },
                new Skill { Id = 5, Name = "Eco-friendly Decoration" }
            );

            modelBuilder.Entity<DecorationStyle>().HasData(
                new DecorationStyle { Id = 1, Name = "Modern" },
                new DecorationStyle { Id = 2, Name = "Traditional" },
                new DecorationStyle { Id = 3, Name = "Coastal" },
                new DecorationStyle { Id = 4, Name = "Scandinavian" },
                new DecorationStyle { Id = 5, Name = "Industrial" }
            );

            modelBuilder.Entity<TicketType>().HasData(
                new TicketType { Id = 1, Type = "Service delay" },
                new TicketType { Id = 2, Type = "Incorrect service implementation" },
                new TicketType { Id = 3, Type = "Unprofessional behavior" },
                new TicketType { Id = 4, Type = "Damage or loss report" },
                new TicketType { Id = 5, Type = "Poor material quality" },
                new TicketType { Id = 6, Type = "Other" }
            );

            modelBuilder.Entity<Offering>().HasData(
                new Offering { Id = 1, Name = "Eco-friendly Package", Description = "Materials and methods that are environmentally friendly" },
                new Offering { Id = 2, Name = "Theme Furniture", Description = "Furniture and items designed to match the selected theme" },
                new Offering { Id = 3, Name = "Consultation Support", Description = "One-on-one consultation support with experts" },
                new Offering { Id = 4, Name = "Color Palette Matching", Description = "Harmonized color scheme across decor elements" },
                new Offering { Id = 5, Name = "Custom Design", Description = "Design on request" },
                new Offering { Id = 6, Name = "Visual Focal Point", Description = "Create a highlight for the space" },
                new Offering { Id = 7, Name = "Artwork & Decor Placement", Description = "Support in selecting and arranging furniture related to the service" }
            );

            modelBuilder.Entity<ScopeOfWork>().HasData(
                new ScopeOfWork { Id = 1, WorkType = "Full Decoration & Styling" },
                new ScopeOfWork { Id = 2, WorkType = "Furniture Selection" },
                new ScopeOfWork { Id = 3, WorkType = "Lighting Setup" },
                new ScopeOfWork { Id = 4, WorkType = "Wall Paint / Wallpaper" },
                new ScopeOfWork { Id = 5, WorkType = "Curtains / Blinds" },
                new ScopeOfWork { Id = 6, WorkType = "Rugs / Carpets" },
                new ScopeOfWork { Id = 7, WorkType = "Wall Art & Decor Items" },
                new ScopeOfWork { Id = 8, WorkType = "Custom Built-ins or Shelving" },
                new ScopeOfWork { Id = 9, WorkType = "Indoor Plants" },
                new ScopeOfWork { Id = 10, WorkType = "Space Optimization" },
                new ScopeOfWork { Id = 11, WorkType = "Decluttering" }
            );
        }
    }
}
