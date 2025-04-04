using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Repository.UnitOfWork;
using static DataAccessObject.Models.Booking;

namespace BusinessLogicLayer.Services.BackgroundJob
{
    public class SurveyDateExpiredJob : IJob
    {
        private readonly ILogger<SurveyDateExpiredJob> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public SurveyDateExpiredJob(ILogger<SurveyDateExpiredJob> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {

                // Lấy các timeslot mà ngày khảo sát đã quá hạn + booking đang ở trạng thái chờ
                var expiredTimeSlots = await _unitOfWork.TimeSlotRepository.Queryable()
                    .Include(ts => ts.Booking)
                    .Where(ts =>
                        ts.SurveyDate < DateTime.Now &&
                        ts.Booking.Status == BookingStatus.Pending)
                    .ToListAsync();

                foreach (var timeSlot in expiredTimeSlots)
                {
                    var booking = timeSlot.Booking;
                    booking.Status = BookingStatus.Canceled;
                    booking.CancelReason = "Survey date expired";
                }

                if (expiredTimeSlots.Any())
                {
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Canceled {Count} bookings with expired survey dates", expiredTimeSlots.Count);
                }
                else
                {
                    _logger.LogInformation("No expired bookings found today.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while canceling expired bookings.");
            }
        }
    }

}