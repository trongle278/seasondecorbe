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
    public class ContractTerminationExpiryJob : IJob
    {
        private readonly ILogger<ContractTerminationExpiryJob> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ContractTerminationExpiryJob(ILogger<ContractTerminationExpiryJob> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("ContractTerminationExpiryJob is running at: {time}", DateTime.Now);

            try
            {
                // Lấy tất cả hợp đồng đã ký và còn cho phép hủy
                var contracts = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Where(c => c.Status == DataAccessObject.Models.Contract.ContractStatus.Signed
                            && c.isTerminatable == true
                            && c.SignedDate.HasValue)
                    .ToListAsync();

                var now = DateTime.Now;
                var updatedCount = 0;

                foreach (var contract in contracts)
                {
                    // Kiểm tra nếu đã quá 3 ngày kể từ khi ký
                    if ((now - contract.SignedDate.Value).TotalDays > 3)
                    {
                        contract.isTerminatable = false;
                        _unitOfWork.ContractRepository.Update(contract);
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Updated {count} contracts to non-terminatable", updatedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ContractTerminationExpiryJob");
                throw;
            }
        }
    }
}
