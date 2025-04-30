using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services.BackgroundJob
{
    public class BookingCancelDisableJob : IJob
    {
        private readonly ILogger<BookingCancelDisableJob> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public BookingCancelDisableJob(ILogger<BookingCancelDisableJob> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await Task.Delay(10000); // chờ app và DB ổn định

                var now = DateTime.Now;
                var thresholdDate = now.AddDays(-2);

                var bookingsToUpdate = await _unitOfWork.BookingRepository.Queryable()
                    .Where(b => (b.CancelDisable == false || b.CancelDisable == null) && b.CreateAt <= thresholdDate)
                    .ToListAsync();

                foreach (var booking in bookingsToUpdate)
                {
                    booking.CancelDisable = true;
                }

                if (bookingsToUpdate.Any())
                {
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Updated {Count} bookings: CancelDisable = true", bookingsToUpdate.Count);
                }
                else
                {
                    _logger.LogInformation("No bookings found that need CancelDisable update.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in BookingCancelDisableJob.");
            }
        }
    }
}
