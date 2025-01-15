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

            var customerRole = await _unitOfWork.RoleRepository
                .Query(r => r.RoleName.ToLower() == "customer")
                .FirstOrDefaultAsync();
            
            if (customerRole == null)
            {
                return new AuthResponse { Success = false, Errors = new List<string> { "Role configuration error" } };
            }

            var account = new Account
            {
                Email = request.Email,
                Password = "",
                FirstName = request.FirstName,
                LastName = request.LastName,
                RoleId = customerRole.Id
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

        public async Task<AuthResponse> GoogleLoginAsync(string credential)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(credential);

                var account = await _unitOfWork.AccountRepository
                    .Query(a => a.Email == payload.Email)
                    .Include(a => a.Role)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    var customerRole = await _unitOfWork.RoleRepository
                        .Query(r => r.RoleName.ToLower() == "customer")
                        .FirstOrDefaultAsync();

                    if (customerRole == null)
                    {
                        return new AuthResponse { Success = false, Errors = new List<string> { "Role configuration error" } };
                    }

                    account = new Account
                    {
                        Email = payload.Email,
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        RoleId = customerRole.Id,
                        Password = "",
                    };

                    await _unitOfWork.AccountRepository.InsertAsync(account);
                    await _unitOfWork.CommitAsync();
                }

                var token = await GenerateJwtToken(account);
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

        private async Task<string> GenerateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var role = await _unitOfWork.RoleRepository.GetByIdAsync(account.RoleId);
            
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
