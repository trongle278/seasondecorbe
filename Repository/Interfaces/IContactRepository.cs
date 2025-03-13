using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Repository.GenericRepository;

namespace Repository.Interfaces
{
    public interface IContactRepository : IGenericRepository<Contact>
    {
        Task<bool> ContactExistsAsync(int userId, int contactId);
        Task<IEnumerable<Contact>> GetUserContactsAsync(int userId);
    }
}
