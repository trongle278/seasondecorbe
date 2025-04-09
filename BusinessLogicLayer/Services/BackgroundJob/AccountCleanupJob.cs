using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class AccountCleanupJob : IJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountCleanupJob> _logger;

        public AccountCleanupJob(IUnitOfWork unitOfWork, ILogger<AccountCleanupJob> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // Chờ 10 giây để app và DB ổn định
            await Task.Delay(10000);

            var unverifiedAccounts = _unitOfWork.AccountRepository
                .Query(a => !a.IsVerified && a.VerificationTokenExpiry < DateTime.Now)
                .ToList();

            if (unverifiedAccounts.Count == 0) return;

            foreach (var account in unverifiedAccounts)
            {
                _unitOfWork.AccountRepository.Delete(account.Id);
            }

            await _unitOfWork.CommitAsync();
            _logger.LogInformation($"Deleted {unverifiedAccounts.Count} unverified accounts.");
        }
    }
}
