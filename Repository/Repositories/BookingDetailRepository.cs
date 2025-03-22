using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Repository.GenericRepository;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class BookingDetailRepository : GenericRepository<BookingDetail>, IBookingDetailRepository
    {
        public BookingDetailRepository(HomeDecorDBContext context) : base(context)
        {
        }
        public async Task InsertRangeAsync(IEnumerable<BookingDetail> bookingDetails)
        {
            await _context.BookingDetails.AddRangeAsync(bookingDetails);
        }
    }
}
