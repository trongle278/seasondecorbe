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

namespace BusinessLogicLayer
{
    public class AuthService: IAuthService
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

            var account = new Account
            {
                Email = request.Email,
                Password = "",// Password will be hashed below
                FirstName = request.FirstName,
                LastName = request.LastName,
                RoleId = 3
            };

            account.Password = HashPassword(account, request.Password);

            await _unitOfWork.AccountRepository.InsertAsync(account);
            await _unitOfWork.CommitAsync();

            return new AuthResponse { Success = true, Token = GenerateJwtToken(account) };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var account = await _unitOfWork.AccountRepository.Query(a => a.Email == request.Email).FirstOrDefaultAsync();
            if (account == null || !VerifyPassword(account, request.Password))
            {
                return new AuthResponse { Success = false, Errors = new List<string> { "Invalid email or password" } };
            }

            return new AuthResponse { Success = true, Token = GenerateJwtToken(account) };
        }

        public async Task<AuthResponse> GoogleLoginAsync(string credential)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(credential);

                // Tìm hoặc tạo user mới
                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == payload.Email)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    account = new Account
                    {
                        Email = payload.Email,
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        RoleId = 3,  // Role Customer
                        Password = "" // Nếu Password không cho phép null
                    };

                    await _unitOfWork.AccountRepository.InsertAsync(account);
                    await _unitOfWork.CommitAsync();
                }

                var token = GenerateJwtToken(account);
                return new AuthResponse { Success = true, Token = token };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message }
                };
            }
        }

        private string GenerateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, account.RoleId == 1 ? "Admin" : "User")
                //CẦN SỬA Ở ĐÂY

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
