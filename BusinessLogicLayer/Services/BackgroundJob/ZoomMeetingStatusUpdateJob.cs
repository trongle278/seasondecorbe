using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services.BackgroundJob
{
    public class ZoomMeetingStatusUpdateJob : IJob
    {
        private readonly ILogger<ZoomMeetingStatusUpdateJob> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ZoomMeetingStatusUpdateJob(ILogger<ZoomMeetingStatusUpdateJob> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await Task.Delay(10000);

                _logger.LogInformation("ZoomMeeting background job started at {Time}", DateTime.Now);

                // Scheduled meetings
                var scheduledMeetings = await GetScheduledMeetingAsync();
                if (scheduledMeetings.Any())
                {
                    await UpdateScheduledMeetingAsync(scheduledMeetings);
                    _logger.LogInformation("Updated {Count} scheduled Zoom meetings to Started status", scheduledMeetings.Count);
                }
                else
                {
                    _logger.LogInformation("No scheduled Zoom meetings to update.");
                }

                // Started meetings
                var startedMeetings = await GetStartedMeetingAsync();
                if (startedMeetings.Any())
                {
                    await UpdateStartedMeetingAsync(startedMeetings);
                    _logger.LogInformation("Updated {Count} started Zoom meetings to Ended status", startedMeetings.Count);
                }
                else
                {
                    _logger.LogInformation("No started Zoom meetings to update.");
                }

                _logger.LogInformation("ZoomMeeting background job completed at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating ZoomMeeting status.");
            }
        }

        private async Task<List<ZoomMeeting>> GetScheduledMeetingAsync()
        {
            var date = DateTime.Now;
            return await _unitOfWork.ZoomRepository.Queryable()
                .Where(z => z.StartTime == date && z.Status == ZoomMeeting.MeetingStatus.Scheduled)
                .ToListAsync();
        }

        private async Task<List<ZoomMeeting>> GetStartedMeetingAsync()
        {
            var date = DateTime.Now;
            return await _unitOfWork.ZoomRepository.Queryable()
                .Where(z =>
                        z.Duration.HasValue &&
                        z.StartTime.AddMinutes(z.Duration.Value) <= date &&
                        z.Status == ZoomMeeting.MeetingStatus.Started)
                .ToListAsync();
        }

        private async Task UpdateScheduledMeetingAsync(List<ZoomMeeting> meetings)
        {
            foreach (var meeting in meetings)
            {
                meeting.Status = ZoomMeeting.MeetingStatus.Started;
            }

            await _unitOfWork.CommitAsync();
        }

        private async Task UpdateStartedMeetingAsync(List<ZoomMeeting> meetings)
        {
            foreach (var meeting in meetings)
            {
                meeting.Status = ZoomMeeting.MeetingStatus.Ended;
            }

            await _unitOfWork.CommitAsync();
        }
    }
}
