using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class DataSeeder : IDataSeeder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Account> _passwordHasher;

        public DataSeeder(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Account>();
        }

        public async Task SeedAdminAsync()
        {
            // Đọc thông tin admin từ appsettings
            var adminEmail = _configuration["Admin:Email"];
            if (string.IsNullOrEmpty(adminEmail))
            {
                // Nếu không có thông tin admin trong cấu hình thì không seed
                return;
            }

            // Kiểm tra xem tài khoản admin đã tồn tại chưa
            var existingAdmin = await _unitOfWork.AccountRepository
                .Query(a => a.Email == adminEmail)
                .FirstOrDefaultAsync();

            int adminId;

            if (existingAdmin == null)
            {
                // Tạo tài khoản admin mới với thông tin mặc định
                var admin = new Account
                {
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "Admin",
                    RoleId = 1,
                    IsVerified = true
                };

                // Hash mật khẩu admin lấy từ cấu hình
                var adminPassword = _configuration["Admin:Password"];
                admin.Password = _passwordHasher.HashPassword(admin, adminPassword);

                await _unitOfWork.AccountRepository.InsertAsync(admin);
                await _unitOfWork.CommitAsync();

                adminId = admin.Id;
            }
            else
            {
                // Nếu admin đã tồn tại, lấy Id
                adminId = existingAdmin.Id;
            }

            // Kiểm tra xem admin đã có ví chưa, nếu chưa thì tạo mới
            var existingWallet = await _unitOfWork.WalletRepository
                .Query(w => w.AccountId == adminId)
                .FirstOrDefaultAsync();

            if (existingWallet == null)
            {
                // Tạo ví cho admin
                var wallet = new Wallet
                {
                    AccountId = adminId,
                    Balance = 0
                };

                await _unitOfWork.WalletRepository.InsertAsync(wallet);
                await _unitOfWork.CommitAsync();
            }
        }
    }
}
