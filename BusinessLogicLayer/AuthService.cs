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

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Account>();
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

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var account = await _unitOfWork.AccountRepository
                .Query(a => a.Email == request.Email)
                .Include(a => a.Role)
                .FirstOrDefaultAsync();

            if (account == null || !VerifyPassword(account, request.Password))
            {
                return new AuthResponse { Success = false, Errors = new List<string> { "Invalid email or password" } };
            }

            return new AuthResponse { Success = true, Token = await GenerateJwtToken(account) };
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
    }
}
