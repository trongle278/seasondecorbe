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
            // Đọc thông tin admin từ appsettings (Admin:Email, Admin:Password)
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

            if (existingAdmin == null)
            {
                // Tạo tài khoản admin mới với thông tin mặc định
                var admin = new Account
                {
                    Email = adminEmail,
                    FirstName = "Admin",   // Bạn có thể tùy chỉnh hoặc đọc thêm từ cấu hình
                    LastName = "Admin",
                    RoleId = 1,            // Giả sử RoleId = 1 ứng với Admin (Role này phải được tạo sẵn)
                    IsVerified = true      // Đánh dấu tài khoản đã xác thực
                };

                // Hash mật khẩu admin lấy từ cấu hình
                var adminPassword = _configuration["Admin:Password"];
                admin.Password = _passwordHasher.HashPassword(admin, adminPassword);

                await _unitOfWork.AccountRepository.InsertAsync(admin);
                await _unitOfWork.CommitAsync();
            }
        }
    }
}
