using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class ProviderResponse
    {
        public bool Success { get; set; }
        public ProviderDTO Data { get; set; }
        public List<string> Errors { get; set; }

        public ProviderResponse()
        {
            Errors = new List<string>();
        }
    }

    public class ProviderDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string Avatar { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool IsProvider { get; set; }
        // Add other properties as needed
    }

    public class ProviderAccountDTO
    {
        public int ProviderId { get; set; }
        public int AccountId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        // Add other properties as needed
    }
}
