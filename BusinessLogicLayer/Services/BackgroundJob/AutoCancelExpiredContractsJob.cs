using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services.BackgroundJob
{
    public class AutoCancelExpiredContractsJob : IJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletService _walletService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _config;
        private readonly ILogger<AutoCancelExpiredContractsJob> _logger;

        public AutoCancelExpiredContractsJob(
            IUnitOfWork unitOfWork,
            IWalletService walletService,
            INotificationService notificationService,
            IConfiguration config,
            ILogger<AutoCancelExpiredContractsJob> logger)
        {
            _unitOfWork = unitOfWork;
            _walletService = walletService;
            _notificationService = notificationService;
            _config = config;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                int timeoutHours = _config.GetValue<int>("ContractSettings:SignatureTimeoutHours", 48);
                var timeoutLimit = DateTime.Now.AddHours(-timeoutHours);

                var expiredContracts = await _unitOfWork.ContractRepository.Queryable()
                    .Include(c => c.Quotation.Booking.DecorService.Account)
                    .Where(c => c.Status == Contract.ContractStatus.Pending &&
                                c.CreatedAt <= timeoutLimit &&
                                c.isSigned != true)
                    .ToListAsync();

                foreach (var contract in expiredContracts)
                {
                    var booking = contract.Quotation.Booking;
                    var provider = booking.DecorService.Account;
                    var providerId = provider.Id;

                    // Lấy commission rate
                    var commissionRate = await _unitOfWork.SettingRepository.Queryable()
                        .Select(s => s.Commission)
                        .FirstOrDefaultAsync();

                    var providerWallet = await _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefaultAsync(w => w.AccountId == providerId);
                    var adminWallet = await _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefaultAsync(w => w.Account.RoleId == 1);

                    if (providerWallet == null || adminWallet == null)
                    {
                        _logger.LogWarning($"Wallet not found for provider #{providerId} or admin.");
                        continue;
                    }

                    decimal total = booking.CommitDepositAmount;
                    decimal adminAmount = total * commissionRate;
                    decimal providerAmount = total - adminAmount;

                    await _walletService.UpdateWallet(adminWallet.Id, adminWallet.Balance + adminAmount);
                    await _walletService.UpdateWallet(providerWallet.Id, providerWallet.Balance - adminAmount);

                    var tx1 = new PaymentTransaction
                    {
                        Amount = providerAmount,
                        BookingId = booking.Id,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.FinalPay
                    };
                    var tx2 = new PaymentTransaction
                    {
                        Amount = adminAmount,
                        BookingId = booking.Id,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.FinalPay
                    };

                    await _unitOfWork.PaymentTransactionRepository.InsertRangeAsync(new[] { tx1, tx2 });

                    // Cập nhật trạng thái
                    contract.Status = Contract.ContractStatus.Rejected;
                    booking.Status = Booking.BookingStatus.Canceled;
                    booking.IsBooked = false;

                    // Gửi thông báo
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = providerId,
                        Title = "Contract Auto-Canceled",
                        Content = $"Contract #{contract.ContractCode} created at {contract.CreatedAt:dd/MM/yyyy HH:mm} has been auto-canceled due to no signature within {timeoutHours} hours.",
                        Url = $"{_config["ClientBaseUrl"]}/seller/booking/{booking.BookingCode}"
                    });

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation($"Contract #{contract.ContractCode} auto-canceled.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during auto-cancel contract job.");
            }
        }
    }

}
