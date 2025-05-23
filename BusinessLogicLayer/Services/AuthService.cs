﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Repository.UnitOfWork;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Apis.Auth;
using System.Numerics;
using Microsoft.AspNetCore.Cors.Infrastructure;
using BusinessLogicLayer.ModelRequest.Cart;
using AutoMapper;

namespace BusinessLogicLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Account> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly ICartService _cartService;
        private readonly IWalletService _walletService;
        private readonly IMapper _mapper;

        public AuthService(IUnitOfWork unitOfWork,
                          IConfiguration configuration,
                          IEmailService emailService,
                          ICartService cartService,
                          IWalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Account>();
            _emailService = emailService;
            _cartService = cartService;
            _walletService = walletService;
        }

        public async Task<BaseResponse> RegisterCustomerAsync(RegisterRequest request)
        {
            try
            {
                var existingAccount = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email)
                    .FirstOrDefaultAsync();

                if (existingAccount != null)
                {
                    if (existingAccount.IsVerified)
                    {
                        return new BaseResponse
                        {
                            Success = false,
                            Errors = new List<string> { "Email already exists" }
                        };
                    }

                    // Nếu chưa xác nhận, gửi lại OTP mới
                    var newOtp = GenerateOTP();
                    existingAccount.VerificationToken = newOtp;
                    existingAccount.VerificationTokenExpiry = DateTime.Now.AddMinutes(15);
                    await _unitOfWork.CommitAsync();

                    await SendVerificationEmail(existingAccount.Email, newOtp);
                    return new BaseResponse
                    {
                        Success = true,
                        Message = "A new verification code has been sent to your email."
                    };
                }

                // Nếu chưa có tài khoản, tạo mới
                var otp = GenerateOTP();
                var account = new Account
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Slug = GenerateDefaultSlug(),
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Location = "",
                    ProvinceCode = "",
                    RoleId = 3, // Customer role
                    IsVerified = false,
                    VerificationToken = otp,
                    VerificationTokenExpiry = DateTime.Now.AddMinutes(15),
                    JoinedDate = DateTime.Now,
                    Reputation = 100
                };

                account.Password = HashPassword(account, request.Password);

                await _unitOfWork.AccountRepository.InsertAsync(account);
                await _unitOfWork.CommitAsync();

                await _cartService.CreateCartAsync(new CartRequest { AccountId = account.Id });
                await _walletService.CreateWallet(account.Id);

                await SendVerificationEmail(account.Email, otp);
                return new BaseResponse
                {
                    Success = true,
                    Message = "Please check your email for verification code"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Registration failed" }
                };
            }
        }

        public async Task<BaseResponse> VerifyEmailAsync(VerifyEmailRequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email &&
                               a.VerificationToken == request.OTP &&
                               !a.IsVerified)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid verification code" }
                    };
                }

                if (account.VerificationTokenExpiry < DateTime.Now)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Verification code expired" }
                    };
                }

                // Verify account
                account.IsVerified = true;
                account.VerificationToken = null;
                account.VerificationTokenExpiry = null;

                await _unitOfWork.CommitAsync();

                return new BaseResponse
                {
                    Success = true,
                    Message = "Email verified successfully"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Success = false,
                    Errors = new List<string> { "Verification failed" }
                };
            }
        }

        private async Task SendVerificationEmail(string email, string otp)
        {
            var emailBody = $@"
        <h2>Verify Your Email</h2>
        <p>Your verification code is: <strong>{otp}</strong></p>
        <p>This code will expire in 15 minutes.</p>
        <p>If you didn't create an account, please ignore this email.</p>";

            await _emailService.SendEmailAsync(
                email,
                "Email Verification",
                emailBody);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var adminEmail = _configuration["Admin:Email"];
            var adminPassword = _configuration["Admin:Password"];

            if (string.Equals(request.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                var adminAccount = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == adminEmail)
                    .Include(a => a.Wallet)
                    .FirstOrDefaultAsync();

                if (adminAccount != null)
                {
                    if (!VerifyPassword(adminAccount, request.Password))
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Errors = new List<string> { "Invalid email or password" }
                        };
                    }

                    if (adminAccount.IsDisable)
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Errors = new List<string> { "Account is banned" }
                        };
                    }
                }
                else
                {
                    if (request.Password != adminPassword)
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Errors = new List<string> { "Invalid email or password" }
                        };
                    }

                    adminAccount = new Account
                    {
                        Email = adminEmail,
                        FirstName = "Admin",
                        LastName = "",
                        RoleId = 1
                    };
                }

                var token = await GenerateJwtToken(adminAccount);
                return new LoginResponse
                {
                    Success = true,
                    Token = token,
                    AccountId = adminAccount.Id,
                    RoleId = adminAccount.RoleId,
                    WalletId = adminAccount.Wallet.Id
                };
            }

            // 👉 **Xử lý đăng nhập người dùng bình thường**
            var account = await _unitOfWork.AccountRepository
                .Query(a => a.Email == request.Email)
                .Include(a => a.Role)
                .Include(a => a.Wallet)  // ✅ Thêm Include để lấy WalletId
                .FirstOrDefaultAsync();

            if (account == null || !VerifyPassword(account, request.Password))
            {
                return new LoginResponse
                {
                    Success = false,
                    Errors = new List<string> { "Invalid email or password" }
                };
            }

            // Kiểm tra tài khoản có bị ban không
            if (account.IsDisable)
            {
                return new LoginResponse
                {
                    Success = false,
                    Errors = new List<string> { "Account is banned" }
                };
            }

            if (account.TwoFactorEnabled)
            {
                var otp = GenerateOTP();
                account.TwoFactorToken = otp;
                account.TwoFactorTokenExpiry = DateTime.Now.AddMinutes(5);
                await _unitOfWork.CommitAsync();

                await SendLoginOTPEmail(account.Email, otp);

                return new LoginResponse
                {
                    Success = true,
                    RequiresTwoFactor = true
                };
            }

            var userToken = await GenerateJwtToken(account);
            return new LoginResponse
            {
                Success = true,
                Token = userToken,
                AccountId = account.Id,
                RoleId = account.RoleId,
                WalletId = account.Wallet.Id  // ✅ Trả WalletId cho người dùng bình thường
            };
        }


        public async Task<LoginResponse> VerifyLoginOTPAsync(VerifyOtpRequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email &&
                               a.TwoFactorToken == request.OTP)
                    .Include(a => a.Role)
                    .Include(a => a.Wallet)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid OTP" }
                    };
                }

                if (account.TwoFactorTokenExpiry < DateTime.Now)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Errors = new List<string> { "OTP has expired" }
                    };
                }

                // Xóa OTP sau khi verify thành công
                account.TwoFactorToken = null;
                account.TwoFactorTokenExpiry = null;
                await _unitOfWork.CommitAsync();

                var token = await GenerateJwtToken(account);
                return new LoginResponse { Success = true, Token = token };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        private async Task SendLoginOTPEmail(string email, string otp)
        {
            var emailBody = $@"
        <h2>Login Verification</h2>
        <p>Your OTP code is: <strong>{otp}</strong></p>
        <p>This code will expire in 5 minutes.</p>
        <p>If you didn't attempt to login, please secure your account immediately.</p>";

            await _emailService.SendEmailAsync(
                email,
                "Login OTP Verification",
                emailBody);
        }

        public async Task<GoogleLoginResponse> GoogleLoginAsync(string idToken)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings());
                var email = payload.Email;
                var firstName = payload.GivenName;
                var lastName = payload.FamilyName;
                var avatar = payload.Picture;

                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == email)
                    .Include(a => a.Role)
                    .Include(a => a.Wallet)
                    .FirstOrDefaultAsync();

                bool isNewUser = false;
                if (account == null)
                {
                    // Nếu chưa có, tạo account mới với slug mặc định
                    account = new Account
                    {
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Password = "",
                        Avatar = avatar,
                        Location = "",
                        ProvinceCode = "",
                        IsVerified = true,
                        RoleId = 3,
                        Slug = GenerateDefaultSlug(),
                        JoinedDate = DateTime.Now,
                        Reputation = 100
                    };

                    await _unitOfWork.AccountRepository.InsertAsync(account);
                    await _unitOfWork.CommitAsync();
                    isNewUser = true;

                    account = await _unitOfWork.AccountRepository
                        .Query(a => a.Email == email)
                        .Include(a => a.Role)
                        .Include(a => a.Wallet)
                        .FirstOrDefaultAsync();
                }

                // Kiểm tra tài khoản có bị ban không
                if (account.IsDisable)
                {
                    return new GoogleLoginResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Account is banned" }
                    };
                }

                if (isNewUser)
                {
                    var cartResponse = await _cartService.CreateCartAsync(new CartRequest { AccountId = account.Id });
                    if (!cartResponse.Success)
                    {
                        return new GoogleLoginResponse
                        {
                            Success = false,
                            Errors = new List<string> { "Google login succeeded, but failed to create cart." }
                        };
                    }

                    var walletSuccess = await _walletService.CreateWallet(account.Id);
                    if (!walletSuccess)
                    {
                        return new GoogleLoginResponse
                        {
                            Success = false,
                            Errors = new List<string> { "Google login succeeded, but failed to create wallet." }
                        };
                    }
                }

                var token = await GenerateJwtToken(account);
                return new GoogleLoginResponse
                {
                    Success = true,
                    Token = token,
                    RoleId = account.RoleId,
                    AccountId = account.Id,
                    WalletId = account.Wallet.Id
                };
            }
            catch (Exception ex)
            {
                return new GoogleLoginResponse
                {
                    Success = false,
                    Errors = new List<string> { "Google login failed", ex.Message }
                };
            }
        }


        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                // Validate email format
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Email is required" }
                    };
                }

                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email.Trim().ToLower())
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "If your email exists in our system, you will receive a password reset OTP"
                    };
                }

                // Kiểm tra xem có OTP cũ chưa hết hạn không
                if (account.ResetPasswordTokenExpiry != null && account.ResetPasswordTokenExpiry > DateTime.Now)
                {
                    var remainingMinutes = Math.Ceiling(((DateTime)account.ResetPasswordTokenExpiry - DateTime.Now).TotalMinutes);
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = $"An OTP has already been sent. Please wait {remainingMinutes} minutes before requesting a new one"
                    };
                }

                var otp = GenerateOTP();
                var otpExpiry = DateTime.Now.AddMinutes(15);

                account.ResetPasswordToken = otp;
                account.ResetPasswordTokenExpiry = otpExpiry;
                await _unitOfWork.CommitAsync();

                await SendResetPasswordEmail(account.Email, otp);

                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "Password reset OTP has been sent to your email. Please check your inbox"
                };
            }
            catch (Exception ex)
            {
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while processing your request. Please try again later" }
                };
            }
        }

        private async Task SendResetPasswordEmail(string email, string otp)
        {
            var emailBody = $@"
        <h2>Reset Your Password</h2>
        <p>Your OTP code is: <strong>{otp}</strong></p>
        <p>This code will expire in 15 minutes.</p>
        <p>If you didn't request this, please ignore this email.</p>
        <p>For security reasons, please do not share this OTP with anyone.</p>";

            await _emailService.SendEmailAsync(
                email,
                "Reset Password OTP",
                emailBody);
        }
        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.ResetPasswordToken == request.OTP)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid OTP" }
                    };
                }

                if (account.ResetPasswordTokenExpiry < DateTime.Now)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Errors = new List<string> { "OTP has expired" }
                    };
                }

                // Kiểm tra password mới không trùng với password cũ
                if (VerifyPassword(account, request.NewPassword))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Errors = new List<string> { "New password must be different from the current password" }
                    };
                }

                account.Password = HashPassword(account, request.NewPassword);
                account.ResetPasswordToken = null;
                account.ResetPasswordTokenExpiry = null;

                await _unitOfWork.CommitAsync();

                return new AuthResponse
                {
                    Success = true,
                    Errors = new List<string> { "Your password has been reset successfully" }
                };
            }
            catch
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while resetting your password" }
                };
            }
        }

        private string HashPassword(Account account, string password)
        {
            return _passwordHasher.HashPassword(account, password);
        }

        private bool VerifyPassword(Account account, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(account, account.Password, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
        public async Task<Toggle2FAResponse> Toggle2FAAsync(int userId, Toggle2FARequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository.GetByIdAsync(userId);
                if (account == null)
                {
                    return new Toggle2FAResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Account not found" }
                    };
                }

                account.TwoFactorEnabled = request.Enable;
                await _unitOfWork.CommitAsync();

                return new Toggle2FAResponse
                {
                    Success = true,
                    TwoFactorEnabled = account.TwoFactorEnabled
                };
            }
            catch (Exception ex)
            {
                return new Toggle2FAResponse
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        #region
        private async Task<string> GenerateJwtToken(Account account)
        {
            Console.WriteLine($"Generating token for account: {account.Email}");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            string roleName;
            // Nếu Id bằng 0, ta xem đây là admin đăng nhập từ appsettings
            if (account.Id == 0)
            {
                roleName = "Admin";
            }
            else
            {
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(account.RoleId);
                if (role == null)
                {
                    throw new Exception("Role not found");
                }
                roleName = role.RoleName;
            }

            Console.WriteLine($"Role determined: {roleName}");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{account.FirstName} {account.LastName}"),
        }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateOTP()
        {
            using var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return (BitConverter.ToUInt32(bytes, 0) % 900000 + 100000).ToString();  // Tạo OTP an toàn hơn
        }

        private string GenerateDefaultSlug()
        {
            // Ví dụ đơn giản: tạo slug là chuỗi số 5 chữ số ngẫu nhiên.
            var random = new Random();
            return random.Next(10000, 99999).ToString();
        }
        #endregion
    }
}
