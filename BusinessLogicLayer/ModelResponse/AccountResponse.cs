using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse
{
    public class AccountDTO
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Slug { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public string? Status { get; set; }
        public int RoleId { get; set; }
        public bool IsProvider { get; set; }
    }

    public class AccountResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
        public AccountDTO Data { get; set; }

        public AccountResponse()
        {
            Errors = new List<string>();
        }
    }

    public class AccountListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
        public List<AccountDTO> Data { get; set; }  // Cho danh sách accounts

        public AccountListResponse()
        {
            Errors = new List<string>();
            Data = new List<AccountDTO>();
        }
    }


}
