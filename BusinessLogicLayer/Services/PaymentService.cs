using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Payment;
using CloudinaryDotNet;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IWalletService _walletService;

        public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IWalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _walletService = walletService;
        }

        // amount = giá booking * comission (0.1)  setiing 
        public async Task<bool> Deposit(int customerId, int providerId, decimal amount, int bookingId)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync()) // Bắt đầu giao dịch
            {
                try
                {
                    // 🔹 Kiểm tra số tiền hợp lệ
                    if (amount <= 0)
                    {
                        throw new Exception("Số tiền đặt cọc phải lớn hơn 0.");
                    }

                    // 🔹 Lấy thông tin ví khách hàng & provider
                    var cusAccount = _unitOfWork.AccountRepository.Queryable()
                        .Include(x => x.Wallet)
                        .FirstOrDefault(x => x.Id == customerId);

                    var providerAccount = _unitOfWork.AccountRepository.Queryable()
                        .Include(x => x.Wallet)
                        .FirstOrDefault(x => x.Id == providerId);

                    if (cusAccount?.Wallet == null || providerAccount?.Wallet == null)
                    {
                        throw new Exception("Ví khách hàng hoặc ví nhà cung cấp không tồn tại.");
                    }

                    var cusWallet = cusAccount.Wallet;
                    var providerWallet = providerAccount.Wallet;

                    // 🔹 Kiểm tra số dư khách hàng
                    if (cusWallet.Balance < amount)
                    {
                        throw new Exception("Số dư khách hàng không đủ để đặt cọc.");
                    }

                    // 🔹 Tạo giao dịch đặt cọc (trạng thái Pending)
                    var depositTransaction = new PaymentTransaction
                    {
                        Amount = amount,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Pending,
                        TransactionType = PaymentTransaction.EnumTransactionType.Deposit,
                        BookingId = bookingId
                    };

                    await _unitOfWork.PaymentTransactionRepository.InsertAsync(depositTransaction);
                    await _unitOfWork.CommitAsync(); // Lưu để lấy ID

                    // 🔹 Cập nhật số dư ví (trừ tiền khách hàng, cộng tiền provider)
                    await _walletService.UpdateWallet(cusWallet.Id, cusWallet.Balance - amount);
                    await _walletService.UpdateWallet(providerWallet.Id, providerWallet.Balance + amount);

                    // 🔹 Lưu giao dịch vào lịch sử ví của khách hàng & Provider
                    var cusWalletTransaction = new WalletTransaction
                    {
                        PaymentTransactionId = depositTransaction.Id,
                        WalletId = cusWallet.Id,
                    };

                    var providerWalletTransaction = new WalletTransaction
                    {
                        PaymentTransactionId = depositTransaction.Id,
                        WalletId = providerWallet.Id,
                    };

                    await _unitOfWork.WalletTransactionRepository.InsertAsync(cusWalletTransaction);
                    await _unitOfWork.WalletTransactionRepository.InsertAsync(providerWalletTransaction);

                    // 🔹 Cập nhật trạng thái giao dịch thành `Success`
                    depositTransaction.TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success;
                    _unitOfWork.PaymentTransactionRepository.Update(depositTransaction);

                    // 🔹 Commit giao dịch
                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public async Task<bool> TopUp(int accountId, decimal amount)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync()) // Bắt đầu giao dịch
            {
                try
                {
                    // Kiểm tra số tiền nạp hợp lệ
                    if (amount <= 0)
                    {
                        throw new Exception("Số tiền nạp phải lớn hơn 0.");
                    }

                    // Lấy thông tin ví
                    var account = _unitOfWork.AccountRepository.Queryable()
                        .Include(x => x.Wallet)
                        .FirstOrDefault(x => x.Id == accountId);

                    if (account?.Wallet == null)
                    {
                        throw new Exception("Ví của tài khoản không tồn tại.");
                    }

                    var wallet = account.Wallet;

                    amount = amount / 100;
                    // Cập nhật số dư ví
                    await _walletService.UpdateWallet(wallet.Id, wallet.Balance + amount);

                    // Tạo giao dịch nạp tiền
                    var newTransaction = new PaymentTransaction
                    {
                        Amount = amount,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.TopUp,
                    };

                    // Tạo bản ghi trong lịch sử giao dịch của ví
                    var newWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = wallet.Id,
                    };

                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newWalletTransaction);

                    // Commit giao dịch
                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public async Task<bool> Refund(int accountId, decimal amount, int bookingId, int adminId)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync()) // Bắt đầu giao dịch
            {
                try
                {
                    // Lấy thông tin ví khách hàng và Admin
                    var cusWallet = _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefault(x => x.AccountId == accountId);
                    var adminWallet = _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefault(x => x.AccountId == adminId);

                    if (cusWallet == null || adminWallet == null)
                    {
                        throw new Exception("Ví khách hàng hoặc ví Admin không tồn tại.");
                    }

                    // Lấy số tiền đã giao dịch trước đó (nếu có)
                    var previousAmount = _unitOfWork.PaymentTransactionRepository.Queryable()
                        .Where(x => x.BookingId == bookingId)
                        .Select(x => x.Amount)
                        .FirstOrDefault();

                    if (amount <= 0 || amount > previousAmount)
                    {
                        throw new Exception("Số tiền hoàn không hợp lệ.");
                    }

                    // Kiểm tra số dư Admin có đủ không
                    if (adminWallet.Balance < amount)
                    {
                        throw new Exception("Số dư ví Admin không đủ để hoàn tiền.");
                    }

                    // Cập nhật số dư ví
                    await _walletService.UpdateWallet(adminWallet.Id, adminWallet.Balance - amount);
                    await _walletService.UpdateWallet(cusWallet.Id, cusWallet.Balance + amount);

                    // Tạo giao dịch hoàn tiền
                    var newTransaction = new PaymentTransaction
                    {
                        Amount = amount,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.Refund,
                        BookingId = bookingId,
                    };

                    // Lưu giao dịch vào lịch sử ví của khách hàng và Admin
                    var newWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = cusWallet.Id,
                    };

                    var newAdminWalletTransaction = new WalletTransaction
                    {
                        PaymentTransaction = newTransaction,
                        WalletId = adminWallet.Id,
                    };

                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newWalletTransaction);
                    await _unitOfWork.WalletTransactionRepository.InsertAsync(newAdminWalletTransaction);

                    // Commit giao dịch
                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public async Task<bool> FinalPay(int accountId, decimal remainBookingAmount, int providerId, int bookingId, decimal commissionRate)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync())
            {
                try
                {
                    var cusWallet = await _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefaultAsync(x => x.AccountId == accountId);
                    var providerWallet = await _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefaultAsync(x => x.AccountId == providerId);
                    var adminWallet = await _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefaultAsync(x => x.AccountId == 1);

                    if (cusWallet == null || providerWallet == null || adminWallet == null)
                        throw new Exception("Ví không tồn tại.");

                    if (remainBookingAmount <= 0)
                        throw new Exception("Số tiền thanh toán không hợp lệ.");

                    if (cusWallet.Balance < remainBookingAmount)
                        throw new Exception("Số dư ví khách hàng không đủ.");

                    // 🔴 Tính toán hoa hồng admin & số tiền provider nhận
                    decimal adminCommission = remainBookingAmount * commissionRate;
                    decimal providerReceiveAmount = remainBookingAmount - adminCommission;

                    // 🔴 Cập nhật số dư ví
                    await _walletService.UpdateWallet(cusWallet.Id, cusWallet.Balance - remainBookingAmount);
                    await _walletService.UpdateWallet(adminWallet.Id, adminWallet.Balance + adminCommission);
                    await _walletService.UpdateWallet(providerWallet.Id, providerWallet.Balance + providerReceiveAmount);

                    // 🔴 Lưu giao dịch thanh toán của khách hàng
                    var paymentTransaction = new PaymentTransaction
                    {
                        Amount = remainBookingAmount,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.FinalPay,
                        BookingId = bookingId,
                    };
                    await _unitOfWork.PaymentTransactionRepository.InsertAsync(paymentTransaction);

                    // 🔴 Lưu giao dịch doanh thu của Admin
                    var adminTransaction = new PaymentTransaction
                    {
                        Amount = adminCommission,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.Revenue, // Loại giao dịch của Admin
                        BookingId = bookingId,
                    };
                    await _unitOfWork.PaymentTransactionRepository.InsertAsync(adminTransaction);

                    // 🔴 Commit giao dịch
                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Lỗi khi thanh toán: {ex.Message}");
                    return false;
                }
            }
        }

        public async Task<bool> OrderPay(int customerId, int providerId, int orderId, decimal amount, decimal commissionRate)
        {
            using (var transaction = await _unitOfWork.BeginTransactionAsync())
            {
                try
                {
                    var customerWallet = await _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefaultAsync(x => x.AccountId == customerId);

                    var providerWallet = await _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefaultAsync(x => x.AccountId == providerId);

                    var adminWallet = await _unitOfWork.WalletRepository.Queryable()
                        .FirstOrDefaultAsync(x => x.AccountId == 1);

                    if (customerWallet == null || providerWallet == null || adminWallet == null)
                        throw new Exception("Ví không tồn tại.");

                    if (amount <= 0)
                        throw new Exception("Số tiền thanh toán không hợp lệ.");

                    if (customerWallet.Balance < amount)
                        throw new Exception("Số dư ví khách hàng không đủ.");

                    // Calculate admin commission and provider income
                    decimal adminCommission = amount * commissionRate;
                    decimal providerReceiveAmount = amount - adminCommission;

                    // Update wallet balance
                    await _walletService.UpdateWallet(customerWallet.Id, customerWallet.Balance - amount);
                    await _walletService.UpdateWallet(adminWallet.Id, adminWallet.Balance + adminCommission);
                    await _walletService.UpdateWallet(providerWallet.Id, providerWallet.Balance + providerReceiveAmount);

                    // Save customer transaction
                    var paymentTransaction = new PaymentTransaction
                    {
                        Amount = providerReceiveAmount,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.OrderPay,
                        OrderId = orderId,
                    };
                    await _unitOfWork.PaymentTransactionRepository.InsertAsync(paymentTransaction);

                    // Save admin revenue transaction
                    var adminTransaction = new PaymentTransaction
                    {
                        Amount = adminCommission,
                        TransactionDate = DateTime.Now,
                        TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                        TransactionType = PaymentTransaction.EnumTransactionType.Revenue, // Loại giao dịch của Admin
                        OrderId = orderId,
                    };
                    await _unitOfWork.PaymentTransactionRepository.InsertAsync(adminTransaction);

                    await _unitOfWork.CommitAsync();
                    transaction.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Lỗi khi thanh toán: {ex.Message}");
                    return false;
                }
            }
        }

        public async Task<BaseResponse<DepositPaymentResponse>> GetDepositPaymentAsync(string contractCode)
        {
            var response = new BaseResponse<DepositPaymentResponse>();

            try
            {
                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Include(c => c.Quotation)  // Bao gồm Quotation liên quan đến contract
                    .ThenInclude(q => q.Booking)
                        .ThenInclude(b => b.Address)
                    .Include(c => c.Quotation.Booking.Account)
                    .Include(c => c.Quotation.Booking.DecorService.Account)
                    .FirstOrDefaultAsync(c => c.ContractCode == contractCode);

                if (contract == null)
                {
                    response.Message = "Contract not found.";
                    return response;
                }

                var quotation = contract.Quotation;  // Lấy Quotation từ contract
                var booking = quotation.Booking;
                var customer = booking.Account;
                var provider = booking.DecorService?.Account;
                var address = booking.Address;

                var depositAmount = booking.TotalPrice * (quotation.DepositPercentage / 100);

                response.Success = true;
                response.Data = new DepositPaymentResponse
                {
                    QuotationCode = quotation.QuotationCode,
                    ContractCode = contract.ContractCode,
                    DepositAmount = depositAmount,

                    CustomerName = $"{customer.LastName} {customer.FirstName}",
                    CustomerEmail = customer.Email,
                    CustomerPhone = customer.Phone,
                    CustomerAddress = address != null
                        ? $"{address.Detail}, {address.Street}, {address.Ward}, {address.District}, {address.Province}"
                        : "",

                    ProviderName = $"{provider.LastName} {provider.FirstName}",
                    ProviderEmail = provider.Email,
                    ProviderPhone = provider.Phone,
                    ProviderAddress = provider.BusinessAddress ?? ""
                };
            }
            catch (Exception ex)
            {
                response.Message = "Failed to retrieve deposit payment info.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<FinalPaymentResponse>> GetFinalPaymentAsync(string contractCode)
        {
            var response = new BaseResponse<FinalPaymentResponse>();

            try
            {
                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Include(c => c.Quotation)  // Bao gồm Quotation liên quan đến contract
                    .ThenInclude(q => q.Booking)
                        .ThenInclude(b => b.Address)
                    .Include(c => c.Quotation.Booking.Account)
                    .Include(c => c.Quotation.Booking.DecorService.Account)
                    .FirstOrDefaultAsync(c => c.ContractCode == contractCode);

                if (contract == null)
                {
                    response.Message = "Contract not found.";
                    return response;
                }

                var quotation = contract.Quotation;  // Lấy Quotation từ contract
                var booking = quotation.Booking;
                var customer = booking.Account;
                var provider = booking.DecorService?.Account;
                var address = booking.Address;

                var depositAmount = booking.TotalPrice * (quotation.DepositPercentage / 100);
                var finalPayment = booking.TotalPrice - depositAmount;

                response.Success = true;
                response.Data = new FinalPaymentResponse
                {
                    QuotationCode = quotation.QuotationCode,
                    ContractCode = contract.ContractCode,
                    FinalPaymentAmount = finalPayment,

                    CustomerName = $"{customer.LastName} {customer.FirstName}",
                    CustomerEmail = customer.Email,
                    CustomerPhone = customer.Phone,
                    CustomerAddress = address != null
                        ? $"{address.Detail}, {address.Street}, {address.Ward}, {address.District}, {address.Province}"
                        : "",

                    ProviderName = $"{provider.LastName} {provider.FirstName}",
                    ProviderEmail = provider.Email,
                    ProviderPhone = provider.Phone,
                    ProviderAddress = provider.BusinessAddress ?? ""
                };
            }
            catch (Exception ex)
            {
                response.Message = "Failed to retrieve final payment info.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}

