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
                // Chờ 10 giây để app và DB ổn định
                await Task.Delay(10000);

                // Lấy các timeslot mà ngày khảo sát đã quá hạn + booking đang ở trạng thái chờ
                var expiredTimeSlots = await _unitOfWork.TimeSlotRepository.Queryable()
                    .Include(ts => ts.Booking)
                        .ThenInclude(b => b.DecorService)
                    .Where(ts =>
                        ts.SurveyDate < DateTime.Now &&
                        ts.Booking.Status == BookingStatus.Planning)
                    .ToListAsync();

                foreach (var timeSlot in expiredTimeSlots)
                {
                    var booking = timeSlot.Booking;

                    // Hủy đơn
                    booking.Status = BookingStatus.Canceled;
                    booking.CancelReason = "Survey date expired";

                    // Trừ điểm provider nếu có
                    var providerId = booking.DecorService?.AccountId;
                    if (providerId.HasValue)
                    {
                        var providerAccount = await _unitOfWork.AccountRepository.GetByIdAsync(providerId.Value);
                        if (providerAccount != null)
                        {
                            providerAccount.Reputation = Math.Max(0, providerAccount.Reputation - 10); // Tránh âm điểm
                        }
                    }
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