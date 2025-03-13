using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Repository.GenericRepository;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class ContactRepository : GenericRepository<Contact>, IContactRepository
    {
        public ContactRepository(HomeDecorDBContext context) : base(context) { }

        public async Task<bool> ContactExistsAsync(int userId, int contactId)
        {
            var contacts = await GetAllAsync(
                limit: 500,
                filter: c => c.UserId == userId && c.ContactId == contactId
            );
            return contacts.Any();
        }

        public async Task<IEnumerable<Contact>> GetUserContactsAsync(int userId)
        {
            return await GetAllAsync(
                limit: 500,
                filter: c => c.UserId == userId,
                includeProperties: new Expression<Func<Contact, object>>[] { c => c.ContactUser }
            );
        }
    }
}
