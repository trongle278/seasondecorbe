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

        public async Task<BaseResponse> RegisterDecoratorAsync(RegisterRequest request)
        {
            try
            {
                var existingAccount = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email)
                    .FirstOrDefaultAsync();

                if (existingAccount != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Email already exists" }
                    };
                }

                var otp = GenerateOTP();
                var account = new Account
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    RoleId = 2, // Decorator role
                    IsVerified = false,
                    VerificationToken = otp,
                    VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15)
                };

                account.Password = HashPassword(account, request.Password);

                await _unitOfWork.AccountRepository.InsertAsync(account);
                await _unitOfWork.CommitAsync();


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

        public async Task<BaseResponse> RegisterCustomerAsync(RegisterRequest request)
        {
            try
            {
                var existingAccount = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == request.Email)
                    .FirstOrDefaultAsync();

                if (existingAccount != null)
                {
                    return new BaseResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Email already exists" }
                    };
                }

                var otp = GenerateOTP();
                var account = new Account
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    RoleId = 3, // Customer role
                    IsVerified = false,
                    VerificationToken = otp,
                    VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                    SubscriptionId = 1
                };

                account.Password = HashPassword(account, request.Password);

                await _unitOfWork.AccountRepository.InsertAsync(account);
                await _unitOfWork.CommitAsync();


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

                if (account.VerificationTokenExpiry < DateTime.UtcNow)
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

        public async Task<GoogleLoginResponse> GoogleLoginAsync(string idToken)
        {
            try
            {
                // Validate the ID token
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings());

                // Extract email and name
                var email = payload.Email;
                var firstName = payload.GivenName;
                var lastName = payload.FamilyName;
                var avatar = payload.Picture;

                // Check if the user already exists in your database
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == email)
                    .Include(a => a.Role) // Ensure role is included
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    // If the user does not exist, create a new account
                    account = new Account
                    {
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Password = "",
                        Avatar = avatar,
                        IsVerified = true,
                        RoleId = 3,
                        SubscriptionId = 1
                    };

                    await _unitOfWork.AccountRepository.InsertAsync(account);
                    await _unitOfWork.CommitAsync();

                    // Reload account to include role
                    account = await _unitOfWork.AccountRepository
                        .Query(a => a.Email == email)
                        .Include(a => a.Role)
                        .FirstOrDefaultAsync();
                }

                // Generate JWT token for the user
                var token = await GenerateJwtToken(account);
                return new GoogleLoginResponse
                {
                    Success = true,
                    Token = token,
                    RoleId = account.RoleId // Return RoleId
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
                if (account.ResetPasswordTokenExpiry != null && account.ResetPasswordTokenExpiry > DateTime.UtcNow)
                {
                    var remainingMinutes = Math.Ceiling(((DateTime)account.ResetPasswordTokenExpiry - DateTime.UtcNow).TotalMinutes);
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = $"An OTP has already been sent. Please wait {remainingMinutes} minutes before requesting a new one"
                    };
                }

                var otp = GenerateOTP();
                var otpExpiry = DateTime.UtcNow.AddMinutes(15);

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
