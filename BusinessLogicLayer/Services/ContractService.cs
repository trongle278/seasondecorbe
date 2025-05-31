using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Repository.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using iText.Layout.Properties;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using static DataAccessObject.Models.Booking;
using System.Globalization;
using System.Text.RegularExpressions;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Html2pdf;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Services
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly string _signatureSecretKey = "super_secret_key";
        private readonly ICloudinaryService _cloudinaryService;
        private readonly INotificationService _notificationService;
        private readonly string _clientBaseUrl;
        private readonly IWalletService _walletService;

        public ContractService(IUnitOfWork unitOfWork, IEmailService emailService, ICloudinaryService cloudinaryService, INotificationService notificationService, IConfiguration configuration, IWalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
            _notificationService = notificationService;
            _clientBaseUrl = configuration["AppSettings:ClientBaseUrl"];
            _walletService = walletService;
        }

        public async Task<BaseResponse<string>> GetContractContentAsync(string contractCode)
        {
            var response = new BaseResponse<string>();

            var contract = await _unitOfWork.ContractRepository
                .Queryable()
                .Include(c => c.Quotation.Booking.Account)
                .Include(c => c.Quotation.Booking.DecorService.Account)
                .Where(c => c.ContractCode == contractCode)
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                response.Message = "Contract not found.";
                return response;
            }

            response.Success = true;
            response.Data = contract.TermOfUseContent;
            return response;
        }

        public async Task<BaseResponse<ContractResponse>> CreateContractByQuotationCodeAsync(string quotationCode, ContractRequest request)
        {
            var response = new BaseResponse<ContractResponse>();
            try
            {
                var quotation = await _unitOfWork.QuotationRepository
                    .Queryable()
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.Account)
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.DecorService)
                            .ThenInclude(ds => ds.Account)
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.TimeSlots)
                    .Include(q => q.LaborDetails)//thêm data vào pdf
                    .Include(q => q.MaterialDetails)//thêm data vào pdf
                    .Where(q => q.QuotationCode == quotationCode)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                var existingContract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Where(c => c.QuotationId == quotation.Id)
                    .FirstOrDefaultAsync();

                if (existingContract != null)
                {
                    response.Message = "Contract already exists.";
                    return response;
                }

                // Update ConstructionDate if passed in request
                if (request.ConstructionDate.HasValue)
                {
                    quotation.Booking.ConstructionDate = request.ConstructionDate.Value;
                    _unitOfWork.BookingRepository.Update(quotation.Booking);
                    await _unitOfWork.CommitAsync();
                }

                // Generate contract content
                var contractContent = GenerateTermOfUseContent(quotation);
                if (string.IsNullOrWhiteSpace(contractContent))
                {
                    response.Message = "Failed to generate contract content.";
                    return response;
                }

                var contract = new Contract
                {
                    ContractCode = GenerateContractCode(),
                    QuotationId = quotation.Id,
                    CreatedAt = DateTime.Now,
                    Status = Contract.ContractStatus.Pending,
                    TermOfUseContent = contractContent,
                    isContractExisted = true,
                    isSigned = false,
                    isDeposited = false,
                    isFinalPaid = false,
                    isTerminatable = true,
                };

                await _unitOfWork.ContractRepository.InsertAsync(contract);
                await _unitOfWork.CommitAsync();

                // PDF Generation
                byte[] pdfBytes;
                try
                {
                    pdfBytes = CreatePdf(contract.TermOfUseContent);
                    if (pdfBytes == null || pdfBytes.Length == 0)
                    {
                        throw new Exception("Generated PDF is empty");
                    }
                }
                catch (Exception pdfEx)
                {
                    Console.WriteLine($"PDF Generation Error: {pdfEx}");
                    response.Success = true;
                    response.Message = "Contract created but PDF generation failed.";
                    response.Data = new ContractResponse
                    {
                        Contract = contract,
                        FileUrl = null
                    };
                    return response;
                }

                // Upload to Cloudinary
                try
                {
                    var fileName = $"{contract.ContractCode}_contract.pdf";
                    var fileStream = new MemoryStream(pdfBytes);
                    var fileUrl = await _cloudinaryService.UploadFileAsync(fileStream, fileName, "application/pdf");

                    contract.ContractFilePath = fileUrl;
                    _unitOfWork.ContractRepository.Update(contract);
                    await _unitOfWork.CommitAsync();

                    response.Success = true;
                    response.Message = "Contract created and uploaded successfully.";
                    response.Data = new ContractResponse
                    {
                        Contract = contract,
                        FileUrl = fileUrl
                    };
                }
                catch (Exception uploadEx)
                {
                    Console.WriteLine($"Cloudinary Upload Error: {uploadEx}");
                    response.Success = true;
                    response.Message = "Contract created but file upload failed.";
                    response.Data = new ContractResponse
                    {
                        Contract = contract,
                        FileUrl = null
                    };
                }

                // Gửi thông báo cho khách hàng
                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = quotation.Booking.AccountId,
                    Title = "Contract Created",
                    Content = $"The contract for booking #{quotation.Booking.BookingCode} has been created. Please read it carefully before signing.",
                    Url = $"{_clientBaseUrl}/quotation/view-contract/{quotationCode}"
                });

                return response;
            }
            catch (Exception ex)
            {
                response.Message = "Failed to create contract.";
                response.Errors.Add(ex.Message);
                if (ex.InnerException != null)
                {
                    response.Errors.Add(ex.InnerException.Message);
                }
                return response;
            }
        }

        public async Task<BaseResponse<string>> RequestSignatureAsync(string contractCode)
        {
            var response = new BaseResponse<string>();

            try
            {
                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Include(c => c.Quotation.Booking.Account)
                    .Where(c => c.ContractCode == contractCode)
                    .FirstOrDefaultAsync();

                if (contract == null)
                {
                    response.Message = "Contract not found.";
                    return response;
                }

                if (contract.Status != Contract.ContractStatus.Pending)
                {
                    response.Message = "Contract has already been processed.";
                    return response;
                }

                var customer = contract.Quotation.Booking.Account;

                var token = GenerateSignatureToken(customer.Id, contract.ContractCode);
                contract.SignatureToken = token;
                contract.SignatureTokenGeneratedAt = DateTime.Now;

                _unitOfWork.ContractRepository.Update(contract);
                await _unitOfWork.CommitAsync();

                var emailContent = GenerateSignatureEmailContent(contract.ContractCode, token);

                await _emailService.SendEmailAsync(
                    customer.Email,
                    "Contract Signature Confirmation",
                    emailContent
                );

                response.Success = true;
                response.Data = null;
                response.Message = "Signature request has been sent to your email.";
            }
            catch (Exception ex)
            {
                response.Message = "Failed to send signature request.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<string>> VerifyContractSignatureAsync(string signatureToken)
        {
            var response = new BaseResponse<string>();

            try
            {
                if (string.IsNullOrWhiteSpace(signatureToken))
                {
                    response.Message = "Signature token is required.";
                    return response;
                }

                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Include(c => c.Quotation)
                    .ThenInclude(q => q.Booking)
                    .Where(c => c.SignatureToken == signatureToken)
                    .FirstOrDefaultAsync();

                if (contract == null)
                {
                    response.Message = "Invalid or expired signature token.";
                    return response;
                }

                var booking = contract.Quotation?.Booking;

                if (contract.Status != Contract.ContractStatus.Pending)
                {
                    response.Message = "This contract has already been processed.";
                    return response;
                }

                if (contract.SignatureTokenGeneratedAt.HasValue &&
                    (DateTime.Now - contract.SignatureTokenGeneratedAt.Value).TotalHours > 24)
                {
                    response.Message = "Signature token has expired.";
                    return response;
                }

                contract.Status = Contract.ContractStatus.Signed;
                contract.SignedDate = DateTime.Now;
                contract.isSigned = true;
                contract.SignatureToken = null;
                contract.SignatureTokenGeneratedAt = null;

                if (booking != null)
                {
                    booking.Status = BookingStatus.Confirm;
                    _unitOfWork.BookingRepository.Update(booking);
                }

                _unitOfWork.ContractRepository.Update(contract);
                await _unitOfWork.CommitAsync();

                string colorbookingCode = $"<span style='color:#5fc1f1;font-weight:bold;'>#{booking.BookingCode}</span>";
                // 🔔 Gửi thông báo cho Provider
                if (booking?.DecorService?.AccountId != null)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = booking.DecorService.AccountId,
                        Title = "Contract Signed",
                        Content = $"Customer has signed the contract for booking #{colorbookingCode}.",
                        Url = $"{_clientBaseUrl}/seller/quotation"
                    });
                }

                response.Success = true;
                response.Message = "Contract has been successfully signed.";
                response.Data = contract.ContractCode;
            }
            catch (Exception ex)
            {
                response.Message = "An error occurred during signature verification.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<ContractFileResponse>> GetContractFileAsync(string quotationCode)
        {
            var response = new BaseResponse<ContractFileResponse>();

            try
            {
                // Lấy Quotation
                var quotation = await _unitOfWork.QuotationRepository
                    .Queryable()
                    .Include(q => q.Booking)
                        .ThenInclude(b => b.Account) // Customer
                    .Include(q => q.Booking.DecorService)
                        .ThenInclude(ds => ds.Account) // Provider
                    .Where(q => q.QuotationCode == quotationCode)
                    .FirstOrDefaultAsync();

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                // Lấy Contract
                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Where(c => c.QuotationId == quotation.Id)
                    .FirstOrDefaultAsync();

                if (contract == null)
                {
                    response.Message = "Contract not found for this quotation.";
                    return response;
                }

                var booking = quotation.Booking;
                if (booking == null)
                {
                    response.Message = "Booking not found for this quotation.";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(contract.ContractFilePath))
                {
                    response.Message = "No contract file has been uploaded.";
                    return response;
                }

                // ✅ Tính tiền đặt cọc
                var depositAmount = booking.TotalPrice * (quotation.DepositPercentage / 100);

                // Lấy thông tin Customer và Provider
                var customer = booking.Account;
                var provider = booking.DecorService?.Account;

                // Nếu thiếu thông tin thì trả lỗi
                if (customer == null || provider == null)
                {
                    response.Message = "Customer or Provider information is missing.";
                    return response;
                }

                var timeslot = await _unitOfWork.TimeSlotRepository
                    .Queryable()
                    .Where(t => t.BookingId == booking.Id)
                    .FirstOrDefaultAsync();

                // ✅ Trả kết quả
                response.Success = true;
                response.Message = "Contract file URL and summary retrieved successfully.";
                response.Data = new ContractFileResponse
                {
                    ContractCode = contract.ContractCode,
                    QuotationCode = contract.Quotation.QuotationCode,
                    Status = (int)contract.Status,
                    IsSigned = contract.isSigned,
                    IsDeposited = contract.isDeposited,
                    IsFinalPaid = contract.isFinalPaid,
                    IsTerminatable = contract.isTerminatable,
                    FileUrl = contract.ContractFilePath,
                    BookingCode = booking.BookingCode,
                    DepositAmount = depositAmount,
                    TotalPrice = booking.TotalPrice,
                    Note = booking.Note,
                    SurveyDate = timeslot.SurveyDate,
                    ConstructionDate = booking.ConstructionDate,                  

                    CustomerName = $"{customer.LastName} {customer.FirstName}",
                    CustomerEmail = customer.Email,
                    CustomerPhone = customer.Phone,
               
                    BusinessName = provider.BusinessName,
                    ProviderName = $"{provider.LastName} {provider.FirstName}",
                    ProviderEmail = provider.Email,
                    ProviderPhone = provider.Phone                  
                    
                };
            }
            catch (Exception ex)
            {
                response.Message = "Failed to get contract file.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        #region Template
        //{quotation.Booking.ExpectedCompletion}
        //.service-details {{
        //    display: flex;
        //    justify-content: space-between;
        //    gap: 20px;
        //}
        //}

//{(quotation.Booking.AdditionalCost.HasValue? $"- Additional Charges: {quotation.Booking.AdditionalCost:N0} VND (based on extra requests)" : "")}
private string GenerateTermOfUseContent(Quotation quotation)
        {
            var customer = quotation.Booking.Account;
            var decorService = quotation.Booking.DecorService;
            var provider = decorService?.Account;

            if (customer == null || provider == null)
            {
                throw new Exception("Invalid quotation data. Customer or provider information is missing.");
            }

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>Home Decoration Service Contract</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            margin: 20px 10%;
            color: #333;
            max-width: 1200px;
        }}
        .logo {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .logo img {{
            height: 60px;
        }}
        h1 {{
            text-align: center;
            color: #2c3e50;
            margin-bottom: 30px;
            font-size: 24px;
        }}
        h2 {{
            color: #2980b9;
            border-bottom: 1px solid #2980b9;
            padding-bottom: 5px;
            margin-top: 25px;
            font-size: 18px;
        }}
        .party-info {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 20px;
        }}
        .party {{
            width: 48%;
            padding: 15px;
            background-color: #f8f9fa;
            border-radius: 5px;
        }}
        .section {{
            margin-bottom: 25px;
        }}
        .service-details {{
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
        }}
        .tasks {{
            width: 48%;
            line-height: 1.8;
        }}
        .materials {{
            width: 48%;
            line-height: 1.8;
        }}
        .signature {{
            display: flex;
            justify-content: space-between;
            margin-top: 50px;
        }}
        .signature .line {{
            border-bottom: 1px solid #2c3e50;
            width: 45%;
            margin-top: 40px;
        }}
        .footer {{
            text-align: center;
            margin-top: 60px;
            font-size: 0.9em;
            color: #7f8c8d;
        }}
        ul {{
            padding-left: 20px;
        }}
        li {{
            margin-bottom: 8px;
        }}
        .indent {{
            margin-left: 20px;
        }}
    </style>
</head>
<body>
    <div class='logo'>
        <img src='https://i.postimg.cc/RFmTf94F/seasondecorlogo.png' alt='SeasonDecor Logo'>
    </div>
    
    <h1>HOME DECORATION SERVICE CONTRACT</h1>

    <div class='section'>
        <h2>1. PARTY INFORMATION</h2>
        <div class='party-info'>
            <div class='party'>
                <strong>Party A (Customer):</strong><br>
                Full Name: {customer.LastName} {customer.FirstName}<br>
                Email: {customer.Email}<br>
                Phone Number: {customer.Phone}<br>
            </div>
            <div class='party'>
                <strong>Party B (Service Provider):</strong><br>
                Service Name: {decorService?.Style}<br>
                Representative: {provider?.LastName} {provider?.FirstName}<br>
                Contact Email: {provider?.Email}<br>
                Phone Number: {provider?.Phone}<br>
            </div>
        </div>
    </div>

    <div class='section'>
        <h2>2. SERVICE DETAILS</h2>
        <p>After reaching an agreement on the quotation, the two parties enter into a construction contract with the following terms:</p>
        
        {(string.IsNullOrWhiteSpace(quotation.Booking.Note) ? "" : $"<p><strong>Customer Note:</strong> {quotation.Booking.Note}</p>")}
        
        <div class='service-details'>
            <div class='tasks'>
                <strong>Labor Task in the Project:</strong>
                <div class='indent'>
                    {(quotation.LaborDetails != null && quotation.LaborDetails.Any()
                                ? string.Join("<br>", quotation.LaborDetails.Select((t, i) =>
                                  $"{i + 1}. {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.TaskName.ToLower())}:" +
                                  $"{(t.Area.HasValue ? $" Area: {t.Area} sqm" : "")}" +
                                  $" - Unit: {t.Unit}"))
                                : "(No work items specified)")}
                </div>
            </div>
            
            <div class='materials'>
                <strong>Materials Used in the Project:</strong>
                <div class='indent'>
                    {(quotation.MaterialDetails != null && quotation.MaterialDetails.Any()
                                ? string.Join("<br>", quotation.MaterialDetails.Select((m, i) =>
                                  $"{i + 1}. {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.MaterialName.ToLower())}:" +
                                  $" - Quantity: {m.Quantity}"))
                                : "(No materials specified)")}
                </div>
            </div>

            <div class='products'>
                <strong>Products Used in the Project:</strong>
                <div class='indent'>
                    {(quotation.ProductDetails != null && quotation.ProductDetails.Any()
                                ? string.Join("<br>", quotation.ProductDetails.Select((p, i) =>
                                  $"{i + 1}. {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(p.ProductName.ToLower())}:" +
                                  $" - Quantity: {p.Quantity}, Unit Price: {p.UnitPrice:N0} VND, Total: {p.TotalPrice:N0} VND"))
                                : "(No products specified)")}
                </div>
            </div>
        </div>
    </div>

    <div class='section'>
        <h2>3. IMPLEMENTATION TIME</h2>
        <ul>
            <li>Start Date: {quotation.Booking.ConstructionDate:dd/MM/yyyy}</li>
            <li>Estimated Completion: 4 - 5 days</li>
        </ul>
    </div>

    <div class='section'>
        <h2>4. COST AND PAYMENT</h2>
        <p>(Via platform wallet)</p>
        <ul>
            <li>Total Cost (includes materials, labor, any additional costs and after deducting commitment fee deposit): {(quotation.Booking.TotalPrice):N0} VND</li>
            <li>Materials deposit from Party A to Party B ({quotation.DepositPercentage}%): {(quotation.DepositPercentage / 100) * (quotation.Booking.TotalPrice):N0} VND</li>
            <li>Remaining Balance: {(quotation.Booking.TotalPrice) - ((quotation.DepositPercentage / 100) * (quotation.Booking.TotalPrice)):N0} VND (Payable upon project completion)</li>
        </ul>
    </div>

    <div class='section'>
        <h2>5. FORCE MAJEURE</h2>
        <ul>
            <li>Force majeure refers to unforeseen events beyond the control of the parties and cannot be prevented despite all necessary measures.</li>
            <li>These include natural disasters (storms, floods, earthquakes, fires), war, terrorism, public unrest, strikes, transportation paralysis, government orders or legal changes beyond the control of the parties.</li>
            <li>In such events, the affected party shall not be liable for breach of contract.</li>
            <li>The affected party must promptly notify the other party to seek remedies and minimize damage.</li>
            <li>If the contract becomes impossible to perform, both parties shall negotiate solutions before contract termination.</li>
            <li>The implementation timeline shall be extended in accordance with the duration and consequences of the force majeure event.</li>
        </ul>
    </div>

    <div class='section'>
        <h2>6. RIGHTS AND OBLIGATIONS OF THE PARTIES</h2>
        <strong>Party A:</strong>
        <ul>
            <li>Provide all necessary documents and site access.</li>
            <li>Coordinate and resolve arising issues during design and construction phases.</li>
            <li>Make payments to Party B in accordance with the contract.</li>
        </ul>

        <strong>Party B:</strong>
        <ul>
            <li>Ensure progress and timely handover of the project as agreed.</li>
            <li>Coordinate with Party A and deliver approved services.</li>
            <li>Provide all tools and equipment for execution.</li>
            <li>Be responsible for workers' meals, water, medication, and health.</li>
            <li>Ensure the quality of provided materials.</li>
            <li>Maintain occupational safety during construction.</li>
            <li>Keep confidentiality of all related documents unless approved by Party A.</li>
            <li>Correct all errors resulting from Party B's design responsibility.</li>
            <li>Maintain site cleanliness.</li>
            <li>If Party A requests design changes after quotation approval, Party A shall bear additional costs accordingly.</li>
            <li>The modification time is included in the contract period as agreed.</li>
            <li>Provide warranty for completed work.</li>
            <li>Assign Mr./Ms. {provider?.LastName} {provider?.FirstName} (Service Representative, Phone: {provider?.Phone}) as the point of contact with Party A.</li>
        </ul>
    </div>

    <div class='section'>
        <h2>7. MODIFICATIONS & ADDITIONAL REQUESTS</h2>
        <ul>
            <li>All new requests must be agreed via the chat system.</li>
            <li>They will be appended as a contract appendix with cost and time adjustments accordingly.</li>
        </ul>
    </div>

    <div class='section'>
        <h2>8. GENERAL TERMS</h2>
        <ul>
            <li>The contract is effective from the signing date until both parties complete the handover and liquidation.</li>
            <li>In the event of a dispute, both parties shall negotiate first before considering legal actions.</li>
            <li>Party B is responsible for providing all necessary documentation related to the project and will be held liable if insufficient.</li>
            <li>Party A agrees not to use the documents provided by Party B for other purposes outside the scope of this contract.</li>
            <li>Both parties commit to fully implementing the terms and conditions of this contract.</li>
            <li>After signing, Party A is not entitled to cancel the contract under any circumstances; any loss or cost arising thereafter shall be borne entirely by Party A.</li>
        </ul>
    </div>
</body>
</html>";
        }

        private string GenerateSignatureEmailContent(string contractCode, string token)
        {
            string verifyUrl = $"{_clientBaseUrl}/sign?token={Uri.EscapeDataString(token)}";
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "SignatureEmailTemplate.html");

            string htmlContent = File.ReadAllText(templatePath);
            htmlContent = htmlContent.Replace("{contractCode}", contractCode)
                                     .Replace("{verifyUrl}", verifyUrl);

            return htmlContent;
        }
        #endregion

        #region
        private string GenerateContractCode()
        {
            return "CON" + DateTime.Now.Ticks;
        }

        private string GenerateSignatureToken(int customerId, string contractCode)
        {
            var secretKey = "super-secret-key-for-hmac"; // có thể đưa vào config
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            var rawData = $"{customerId}:{contractCode}:{timestamp}";
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var dataBytes = Encoding.UTF8.GetBytes(rawData);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            var hash = Convert.ToBase64String(hashBytes);

            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{hash}|{timestamp}|{contractCode}"));
        }

        private byte[] CreatePdf(string htmlContent)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);

                // Set up converter properties
                var converterProperties = new ConverterProperties();

                // Convert HTML to PDF
                HtmlConverter.ConvertToPdf(htmlContent, pdf, converterProperties);

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF Generation Error: {ex}");
                throw;
            }
        }
        #endregion

        #region Mobile
        private string GenerateMobileSignatureEmailContent(string contractCode, string token)
        {
            // Thay vì trực tiếp sử dụng URI scheme, chuyển hướng qua API endpoint trước
            //string verifyUrl = $"https://f257-2001-ee1-e802-6da0-f99e-a008-39ac-4621.ngrok-free.app/api/Contract/verify-signature-mobile/{Uri.EscapeDataString(token)}";
            string verifyUrl = $"https://seasondecor.azurewebsites.net/api/Contract/verify-signature-mobile/{Uri.EscapeDataString(token)}";

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "SignatureEmailTemplate.html");

            string htmlContent = File.ReadAllText(templatePath);
            htmlContent = htmlContent.Replace("{contractCode}", contractCode)
                                     .Replace("{verifyUrl}", verifyUrl);

            return htmlContent;
        }

        public async Task<BaseResponse<string>> RequestSignatureForMobileAsync(string contractCode)
        {
            var response = new BaseResponse<string>();

            try
            {
                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Include(c => c.Quotation.Booking.Account)
                    .Where(c => c.ContractCode == contractCode)
                    .FirstOrDefaultAsync();

                if (contract == null)
                {
                    response.Message = "Contract not found.";
                    return response;
                }

                if (contract.Status != Contract.ContractStatus.Pending)
                {
                    response.Message = "Contract has already been processed.";
                    return response;
                }

                var customer = contract.Quotation.Booking.Account;

                var token = GenerateSignatureToken(customer.Id, contract.ContractCode);
                contract.SignatureToken = token;
                contract.SignatureTokenGeneratedAt = DateTime.Now;

                _unitOfWork.ContractRepository.Update(contract);
                await _unitOfWork.CommitAsync();

                var emailContent = GenerateMobileSignatureEmailContent(contract.ContractCode, token);

                await _emailService.SendEmailAsync(
                    customer.Email,
                    "Contract Signature Confirmation",
                    emailContent
                );

                response.Success = true;
                response.Message = "Signature request has been sent to your email (mobile version).";
            }
            catch (Exception ex)
            {
                response.Message = "Failed to send mobile signature request.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        #endregion

        //test
        //public async Task<BaseResponse> TerminateContractAsync(string contractCode,TerminationType type, string reason, decimal? penaltyFee = null)
        //hủy đơn phương

        public async Task<BaseResponse<string>> RequestTerminateOtpAsync(string contractCode)
        {
            var response = new BaseResponse<string>();

            var contract = await _unitOfWork.ContractRepository
                .Queryable()
                .Include(c => c.Quotation.Booking.Account)
                .FirstOrDefaultAsync(c => c.ContractCode == contractCode);

            if (contract == null)
            {
                response.Message = "Contract not found";
                return response;
            }

            if (contract.Status != Contract.ContractStatus.Signed)
            {
                response.Message = "Contract is not in a signed state";
                return response;
            }

            var otp = new Random().Next(100000, 999999).ToString(); // 6-digit OTP
            contract.TerminationOtp = otp;
            contract.TerminationOtpGeneratedAt = DateTime.Now;

            await _unitOfWork.CommitAsync();

            var email = contract.Quotation.Booking.Account.Email;
            var subject = "Contract Termination OTP";
            var body = $@"
<div style=""font-family: Arial, sans-serif; padding: 20px; background-color: #f9f9f9; border: 1px solid #ddd;"">
    <h2 style=""color: #333;"">Contract Termination OTP</h2>
    <p>Dear Customer,</p>
    <p>You have requested to terminate your contract. Please use the OTP code below to confirm this action:</p>
    <p style=""font-size: 24px; font-weight: bold; color: #d32f2f;"">{otp}</p>
    <p>This OTP is valid for <strong>10 minutes</strong>.</p>
    <hr />
    <p style=""font-size: 12px; color: #777;"">If you did not request this, please ignore this email.</p>
</div>";


            await _emailService.SendEmailAsync(email, subject, body);

            response.Success = true;
            response.Data = "OTP sent to your email";
            return response;
        }

        public async Task<BaseResponse> TerminateContract(string contractCode, string otp)
        {
            var response = new BaseResponse();
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. Lấy thông tin hợp đồng
                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Include(c => c.Quotation.Booking)
                        .ThenInclude(b => b.Account.Wallet)
                    .Include(c => c.Quotation.Booking.DecorService.Account.Wallet)
                    .FirstOrDefaultAsync(c => c.ContractCode == contractCode);

                if (contract == null)
                {
                    response.Message = "Contract not found";
                    return response;
                }

                // Chỉ cần kiểm tra IsTerminatable, không cần kiểm tra ngày nữa
                if (contract.isTerminatable == false)
                {
                    response.Message = "Contract can no longer be terminated (3-day period has expired)";
                    return response;
                }

                if (contract?.Status != Contract.ContractStatus.Signed)
                {
                    response.Message = "Contract not found or not signed";
                    return response;
                }

                if (string.IsNullOrEmpty(contract.TerminationOtp) || contract.TerminationOtpGeneratedAt == null)
                {
                    response.Message = "No termination OTP has been requested";
                    return response;
                }

                if (contract.TerminationOtp != otp || DateTime.Now > contract.TerminationOtpGeneratedAt.Value.AddMinutes(10))
                {
                    response.Message = "Invalid or expired OTP";
                    return response;
                }

                var booking = contract.Quotation.Booking;
                decimal penaltyAmount = booking.TotalPrice * 0.5m;

                // 2. Kiểm tra số dư
                if (booking.Account.Wallet.Balance < penaltyAmount)
                {
                    response.Message = "Customer wallet balance insufficient for penalty";
                    return response;
                }

                //// 3. Tạo transaction phạt
                //var penaltyTransaction = new PaymentTransaction
                //{
                //    Amount = penaltyAmount,
                //    TransactionDate = DateTime.Now,
                //    TransactionType = PaymentTransaction.EnumTransactionType.PenaltyPay,
                //    TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                //    BookingId = booking.Id
                //};

                // 3. Tạo transaction phạt (của khách hàng)
                var customerPenaltyTransaction = new PaymentTransaction
                {
                    Amount = penaltyAmount,
                    TransactionDate = DateTime.Now,
                    TransactionType = PaymentTransaction.EnumTransactionType.PenaltyPay,
                    TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                    BookingId = booking.Id
                };
                await _unitOfWork.PaymentTransactionRepository.InsertAsync(customerPenaltyTransaction);

                // 4. Tạo transaction nhận tiền (của provider)
                var providerRevenueTransaction = new PaymentTransaction
                {
                    Amount = penaltyAmount,
                    TransactionDate = DateTime.Now,
                    TransactionType = PaymentTransaction.EnumTransactionType.Revenue,
                    TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                    BookingId = booking.Id
                };
                await _unitOfWork.PaymentTransactionRepository.InsertAsync(providerRevenueTransaction);

                await _unitOfWork.CommitAsync();

                // 5. Trừ tiền khách, cộng tiền provider
                booking.Account.Wallet.Balance -= penaltyAmount;
                booking.DecorService.Account.Wallet.Balance += penaltyAmount;

                // 6. Lưu lịch sử giao dịch
                await _unitOfWork.WalletTransactionRepository.InsertAsync(new WalletTransaction
                {
                    WalletId = booking.Account.Wallet.Id,
                    PaymentTransactionId = customerPenaltyTransaction.Id
                });

                await _unitOfWork.WalletTransactionRepository.InsertAsync(new WalletTransaction
                {
                    WalletId = booking.DecorService.Account.Wallet.Id,
                    PaymentTransactionId = providerRevenueTransaction.Id
                });

                // 6. Cập nhật trạng thái hợp đồng
                contract.Status = Contract.ContractStatus.Canceled;
                contract.TerminationOtp = null;
                contract.TerminationOtpGeneratedAt = null;
                contract.Quotation.Status = Quotation.QuotationStatus.Closed;
                booking.Status = BookingStatus.Canceled;
                booking.HasTerminated = true;
                booking.IsBooked = false;

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // ✅ Gửi email cho customer
                await _emailService.SendEmailAsync(
                    booking.Account.Email,
                    "Contract Termination Confirmation",
                    $"Dear {booking.Account.FirstName} {booking.Account.LastName},<br/><br/>" +
                    $"Your contract with code <b>{contract.ContractCode}</b> has been terminated successfully. " +
                    $"A penalty fee of <b>{penaltyAmount:C}</b> has been deducted from your wallet.<br/><br/>Thank you."
                );

                // ✅ Gửi email cho provider
                await _emailService.SendEmailAsync(
                    booking.DecorService.Account.Email,
                    "Contract Terminated",
                    $"Dear {booking.DecorService.Account.FirstName} {booking.DecorService.Account.LastName},<br/><br/>" +
                    $"The contract with code <b>{contract.ContractCode}</b> has been terminated by the customer. " +
                    $"You have received a compensation amount of <b>{penaltyAmount:C}</b> in your wallet.<br/><br/>Please check your balance."
                );

                response.Success = true;
                response.Message = "Contract terminated successful";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.Message = $"Termination failed: {ex.Message}";
            }

            return response;
        }

        //hủy 2 bên
        public async Task<BaseResponse> RequestCancelContractAsync(string contractCode, int cancelReasonId, string cancelReason)
        {
            var response = new BaseResponse();
            try
            {
                // Lấy thông tin hợp đồng
                var contract = await _unitOfWork.ContractRepository.Queryable()
                    .Include(c => c.Quotation.Booking.DecorService)
                    .FirstOrDefaultAsync(c => c.ContractCode == contractCode);

                if (contract == null)
                {
                    response.Message = "Contract not found or not eligible for cancellation";
                    return response;
                }

                // Validate lý do hủy
                var validReason = await _unitOfWork.CancelTypeRepository.Queryable()
                    .AnyAsync(c => c.Id == cancelReasonId);

                if (!validReason)
                {
                    response.Message = "Invalid cancellation reason";
                    return response;
                }

                // Cập nhật trạng thái chờ hủy
                contract.Status = Contract.ContractStatus.PendingCancel;
                contract.Quotation.Booking.CancelTypeId = cancelReasonId;
                contract.Reason = cancelReason;

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Cancellation request submitted";
            }
            catch (Exception ex)
            {
                response.Message = $"Request failed: {ex.Message}";
            }
            return response;
        }

        public async Task<BaseResponse> ApproveCancelContractAsync(string contractCode)
        {
            var response = new BaseResponse();
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Lấy hợp đồng cần huỷ
                var contract = await _unitOfWork.ContractRepository.Queryable()
                    .Include(c => c.Quotation.Booking)
                        .ThenInclude(b => b.DecorService.Account)
                    .FirstOrDefaultAsync(c => c.ContractCode == contractCode && c.Status == Contract.ContractStatus.PendingCancel);

                if (contract?.Quotation?.Booking == null)
                {
                    response.Message = "Contract not found or not in cancellable state.";
                    return response;
                }

                var booking = contract.Quotation.Booking;
                var providerId = booking.DecorService.AccountId;

                // Lấy % hoa hồng
                var commissionRate = await _unitOfWork.SettingRepository.Queryable()
                    .Select(s => s.Commission)
                    .FirstOrDefaultAsync();

                // Lấy ví
                var providerWallet = await _unitOfWork.WalletRepository.Queryable()
                    .FirstOrDefaultAsync(w => w.AccountId == providerId);

                var adminWallet = await _unitOfWork.WalletRepository.Queryable()
                    .FirstOrDefaultAsync(w => w.Account.RoleId == 1);

                if (providerWallet == null || adminWallet == null)
                {
                    response.Message = "Provider or Admin wallet not found.";
                    return response;
                }

                // Tính toán hoàn tiền
                decimal totalAmountToRefund = booking.CommitDepositAmount;
                decimal adminCommissionAmount = totalAmountToRefund * commissionRate;
                decimal providerAmount = totalAmountToRefund - adminCommissionAmount;

                // Cập nhật ví
                await _walletService.UpdateWallet(adminWallet.Id, adminWallet.Balance + adminCommissionAmount);
                await _walletService.UpdateWallet(providerWallet.Id, providerWallet.Balance - adminCommissionAmount);

                // Ghi giao dịch
                var providerRefundTransaction = new PaymentTransaction
                {
                    Amount = providerAmount,
                    TransactionDate = DateTime.Now,
                    TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                    TransactionType = PaymentTransaction.EnumTransactionType.FinalPay,
                    BookingId = booking.Id
                };
                await _unitOfWork.PaymentTransactionRepository.InsertAsync(providerRefundTransaction);

                var adminRefundTransaction = new PaymentTransaction
                {
                    Amount = adminCommissionAmount,
                    TransactionDate = DateTime.Now,
                    TransactionStatus = PaymentTransaction.EnumTransactionStatus.Success,
                    TransactionType = PaymentTransaction.EnumTransactionType.FinalPay,
                    BookingId = booking.Id
                };
                await _unitOfWork.PaymentTransactionRepository.InsertAsync(adminRefundTransaction);

                // Cập nhật trạng thái
                contract.Status = Contract.ContractStatus.Canceled;
                booking.Status = Booking.BookingStatus.Canceled;
                booking.IsBooked = false;

                await _unitOfWork.CommitAsync();

                // Gửi thông báo
                string providerUrl = ""; // frontend URL cho Provider
                string adminUrl = "";    // frontend URL cho Admin

                await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                {
                    AccountId = providerId,
                    Title = "Contract Canceled",
                    Content = $"The customer has canceled contract #{contractCode}.",
                    Url = providerUrl
                });

                var adminIds = await _unitOfWork.AccountRepository.Queryable()
                    .Where(a => a.RoleId == 1)
                    .Select(a => a.Id)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                {
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        AccountId = adminId,
                        Title = "Revenue Notice",
                        Content = $"You have been credited with an additional amount in your income.",
                        Url = adminUrl
                    });
                }

                await transaction.CommitAsync();
                response.Success = true;
                response.Message = "Contract cancelled successfully.";
                response.Data = booking;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.Success = false;
                response.Message = "Failed to cancel the contract.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse<ContractCancelDetailResponse>> GetRequestContractCancelDetailAsync(string contractCode)
        {
            var response = new BaseResponse<ContractCancelDetailResponse>();
            try
            {
                // Get contract with cancellation details
                var contract = await _unitOfWork.ContractRepository.Queryable()
                    .Include(c => c.Quotation.Booking)
                        .ThenInclude(b => b.CancelType)
                    .FirstOrDefaultAsync(c => c.ContractCode == contractCode &&
                                             c.Status == Contract.ContractStatus.PendingCancel);

                if (contract == null)
                {
                    response.Message = "Contract not found or not in cancellation pending state";
                    return response;
                }

                var result = new ContractCancelDetailResponse
                {
                    ContractCode = contract.ContractCode,
                    Status = (int)contract.Status,
                    CancelType = contract.Quotation.Booking.CancelType?.Type,
                    Reason = contract.Quotation.Booking.CancelReason
                };

                response.Success = true;
                response.Message = "Contract cancellation details retrieved successfully";
                response.Data = result;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to get contract cancellation details";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        public async Task<BaseResponse> TriggerAllContractTerminatableAsync()
        {
            var response = new BaseResponse();

            try
            {
                // Lấy những hợp đồng hợp lệ để cập nhật
                var contracts = await _unitOfWork.ContractRepository.Queryable()
                    .Include(c => c.Quotation.Booking)
                    .Where(c => c.Status == Contract.ContractStatus.Signed && c.isTerminatable == true)
                    .ToListAsync();

                foreach (var contract in contracts)
                {
                    // Điều kiện: đã ký, chưa huỷ
                    contract.isTerminatable = false;
                }

                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Contract terminatable statuses updated successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error updating contract terminatable statuses.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

        //test
        public async Task<BaseResponse> SetTerminatableByContractCodeAsync(string contractCode)
        {
            var response = new BaseResponse();

            try
            {
                var contract = await _unitOfWork.ContractRepository.Queryable()
                    .FirstOrDefaultAsync(c => c.ContractCode == contractCode);

                if (contract == null)
                {
                    response.Success = false;
                    response.Message = "Contract not found.";
                    return response;
                }

                // Điều kiện cho phép kết thúc
                if (contract.Status == Contract.ContractStatus.Signed)
                {
                    contract.isTerminatable = false;
                    await _unitOfWork.CommitAsync();

                    response.Success = true;
                    response.Message = $"Contract marked as terminatable.";
                }
                else
                {
                    response.Success = false;
                    response.Message = "Contract is not in a valid state to allow termination.";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Failed to update contract terminatable flag.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }

    }
}
