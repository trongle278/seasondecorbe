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
    public class AccountCleanupService : IJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountCleanupService> _logger;

        public AccountCleanupService(IUnitOfWork unitOfWork, ILogger<AccountCleanupService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
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
