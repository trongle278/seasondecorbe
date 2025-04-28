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
    public class DecorServiceStatusUpdateJob: IJob
    {
        private readonly ILogger<DecorServiceStatusUpdateJob> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public DecorServiceStatusUpdateJob(ILogger<DecorServiceStatusUpdateJob> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                // Chờ 10 giây để app và DB ổn định (nếu cần)
                await Task.Delay(10000);

                // Lấy các DecorService có StartDate là ngày hôm nay và đang ở trạng thái NotAvailable
                var servicesToUpdate = await GetServicesToUpdateAsync();

                if (!servicesToUpdate.Any())
                {
                    _logger.LogInformation("No DecorServices need to be updated today.");
                    return;
                }

                // Cập nhật trạng thái của các DecorService
                await UpdateDecorServicesAsync(servicesToUpdate);

                _logger.LogInformation("Updated {Count} DecorServices to Available status", servicesToUpdate.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating DecorService status.");
            }
        }

        // Lấy các DecorService có StartDate là ngày hôm nay và đang ở trạng thái NotAvailable
        private async Task<List<DecorService>> GetServicesToUpdateAsync()
        {
            var today = DateTime.Today;
            return await _unitOfWork.DecorServiceRepository.Queryable()
                .Where(ds => ds.StartDate.Date == today && ds.Status == DecorService.DecorServiceStatus.NotAvailable)
                .ToListAsync();
        }

        // Cập nhật trạng thái của các DecorService
        private async Task UpdateDecorServicesAsync(List<DecorService> servicesToUpdate)
        {
            foreach (var service in servicesToUpdate)
            {
                service.Status = DecorService.DecorServiceStatus.Available;
            }

            // Lưu thay đổi vào cơ sở dữ liệu
            await _unitOfWork.CommitAsync();
        }
    }
}
