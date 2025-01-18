using System;
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

namespace BusinessLogicLayer
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Account> _passwordHasher;
        private readonly IEmailService _emailService;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Account>();
            _emailService = emailService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingAccount = await _unitOfWork.AccountRepository.Query(a => a.Email == request.Email).FirstOrDefaultAsync();
            if (existingAccount != null)
            {
                return new AuthResponse { Success = false, Errors = new List<string> { "Email already exists" } };
            }

            if (request.RoleId <= 0)
            {
                return new AuthResponse { Success = false, Errors = new List<string> { "Role must be selected" } };
            }

            var account = new Account
            {
                Email = request.Email,
                Password = "",
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = request.Gender,
                Phone = request.Phone,
                Address = request.Address,
                RoleId = request.RoleId
            };

            account.Password = HashPassword(account, request.Password);

            await _unitOfWork.AccountRepository.InsertAsync(account);
            await _unitOfWork.CommitAsync();

            return new AuthResponse { Success = true, Token = await GenerateJwtToken(account) };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email)
                    .Include(a => a.Role)
                    .FirstOrDefaultAsync();

                if (account == null || !VerifyPassword(account, request.Password))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid email or password" }
                    };
                }

                // Admin không cần OTP
                if (account.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    var adminToken = await GenerateJwtToken(account);
                    return new LoginResponse { Success = true, Token = adminToken };
                }

                // User khác có bật 2FA
                if (account.TwoFactorEnabled)
                {
                    var otp = GenerateOTP();
                    account.TwoFactorToken = otp;
                    account.TwoFactorTokenExpiry = DateTime.UtcNow.AddMinutes(5);
                    await _unitOfWork.CommitAsync();

                    await SendLoginOTPEmail(account.Email, otp);

                    return new LoginResponse
                    {
                        Success = true,
                        RequiresTwoFactor = true
                    };
                }

                // User không bật 2FA
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

        public async Task<LoginResponse> VerifyLoginOTPAsync(VerifyOtpRequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email &&
                               a.TwoFactorToken == request.OTP)
                    .Include(a => a.Role)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid OTP" }
                    };
                }

                if (account.TwoFactorTokenExpiry < DateTime.UtcNow)
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

        public async Task<GoogleLoginResponse> GoogleLoginAsync(string credential, int? roleId = null)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(credential);

                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == payload.Email)
                    .Include(a => a.Role)
                    .FirstOrDefaultAsync();

                // Nếu tài khoản đã tồn tại, trả về token luôn
                if (account != null)
                {
                    var token = await GenerateJwtToken(account);
                    return new GoogleLoginResponse { Success = true, Token = token };
                }

                // Xử lý cho user mới (first time login)
                if (!roleId.HasValue || roleId <= 0)
                {
                    return new GoogleLoginResponse
                    {
                        Success = false,
                        Errors = new List<string> { "First time login requires role selection" },
                        IsFirstLogin = true
                    };
                }

                // Tạo tài khoản mới với role đã chọn
                account = new Account
                {
                    Email = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    RoleId = roleId.Value,
                    Password = "",
                };

                await _unitOfWork.AccountRepository.InsertAsync(account);
                await _unitOfWork.CommitAsync();

                var newToken = await GenerateJwtToken(account);
                return new GoogleLoginResponse { Success = true, Token = newToken };
            }
            catch (Exception ex)
            {
                return new GoogleLoginResponse
                {
                    Success = false,
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message }
                };
            }
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email.Trim().ToLower())  // Chuẩn hóa email
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Errors = new List<string> { "If the email exists, you will receive a password reset OTP" }  // Message an toàn hơn
                    };
                }

                // Kiểm tra xem có OTP cũ chưa hết hạn không
                if (account.ResetPasswordTokenExpiry != null && account.ResetPasswordTokenExpiry > DateTime.UtcNow)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Please wait for the current OTP to expire or use it to reset your password" }
                    };
                }

                var otp = GenerateOTP();
                var otpExpiry = DateTime.UtcNow.AddMinutes(15);

                account.ResetPasswordToken = otp;
                account.ResetPasswordTokenExpiry = otpExpiry;
                await _unitOfWork.CommitAsync();

                await SendResetPasswordEmail(account.Email, otp);

                return new AuthResponse { Success = true };
            }
            catch
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while processing your request" }  // Message chung
                };
            }
        }

        private string GenerateOTP()
        {
            using var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return (BitConverter.ToUInt32(bytes, 0) % 900000 + 100000).ToString();  // Tạo OTP an toàn hơn
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

                if (account.ResetPasswordTokenExpiry < DateTime.UtcNow)
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

                return new AuthResponse { Success = true };
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

        private async Task<string> GenerateJwtToken(Account account)
        {
            // Thêm logging để debug
            Console.WriteLine($"Generating token for account: {account.Email}");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            // Kiểm tra role
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(account.RoleId);
            if (role == null)
            {
                throw new Exception("Role not found");
            }
            Console.WriteLine($"Role found: {role.RoleName}");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, role.RoleName),
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{account.FirstName} {account.LastName}"),
        }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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
    }
}
