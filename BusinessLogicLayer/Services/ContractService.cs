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

namespace BusinessLogicLayer.Services
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly string _signatureSecretKey = "super_secret_key";
        private readonly ICloudinaryService _cloudinaryService;

        public ContractService(IUnitOfWork unitOfWork, IEmailService emailService, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<BaseResponse<string>> GetContractContentAsync(string contractCode)
        {
            var response = new BaseResponse<string>();

            var contract = await _unitOfWork.ContractRepository
                .Queryable()
                .Include(c => c.Quotation.Booking.Account)
                .Include(c => c.Quotation.Booking.DecorService.Account)
                .FirstOrDefaultAsync(c => c.ContractCode == contractCode);

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
                    .FirstOrDefaultAsync(q => q.QuotationCode == quotationCode);

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                var existingContract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .FirstOrDefaultAsync(c => c.QuotationId == quotation.Id);

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
                    isSigned = false
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
                    .FirstOrDefaultAsync(c => c.ContractCode == contractCode);

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
                    .FirstOrDefaultAsync(c => c.SignatureToken == signatureToken);

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
                    .FirstOrDefaultAsync(q => q.QuotationCode == quotationCode);

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                // Lấy Contract
                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .FirstOrDefaultAsync(c => c.QuotationId == quotation.Id);

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
                    Status = (int)contract.Status,
                    IsSigned = contract.isSigned,
                    FileUrl = contract.ContractFilePath,
                    BookingCode = booking.BookingCode,
                    DepositAmount = depositAmount,
                    Note = booking.Note ?? "",
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

            return
        $@"
1. PARTY INFORMATION:
   - Party A (Customer):
     + Full Name: {customer.LastName} {customer.FirstName}
     + Email: {customer.Email}
     + Phone Number: {customer.Phone}

   - Party B (Service Provider):
     + Service Name: {decorService?.Style}
     + Representative: {provider?.LastName} {provider?.FirstName}
     + Contact Email: {provider?.Email}
     + Phone Number: {provider?.Phone}

After reaching an agreement on the quotation, the two parties enter into a construction contract with the following terms:

2. SERVICE DETAILS:
   - Party B shall provide decoration services requested by Party A at the following address: {quotation.Booking.Address}

   * Scope of Work: Party B will carry out the following tasks:


   {(quotation.LaborDetails != null && quotation.LaborDetails.Any()
            ? string.Join("\n", quotation.LaborDetails.Select((t, i) =>
              $"     {i + 1}. {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.TaskName.ToLower())}:" +
              $"{(t.Area.HasValue ? $" Area: {t.Area} sqm" : "")}" +
              $" - Unit: {t.Unit}"))
            : "     (No work items specified)")}

   * Materials Used in the Project:


   {(quotation.MaterialDetails != null && quotation.MaterialDetails.Any()
            ? string.Join("\n", quotation.MaterialDetails.Select((m, i) =>
              $"     {i + 1}. {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.MaterialName.ToLower())}:" +
              $" - Quantity: {m.Quantity}"))
            : "     (No materials specified)")}

3. IMPLEMENTATION TIME:
   - Start Date: {quotation.Booking.ConstructionDate:dd/MM/yyyy}
   - Estimated Completion as per customer’s request: 3 - 5 days

4. COST AND PAYMENT: (Via platform wallet)
   - Total Cost (includes materials, labor, and any additional costs): {(quotation.MaterialCost + quotation.ConstructionCost):N0} VND
   - Deposit from Party A to Party B ({quotation.DepositPercentage}%): {(quotation.DepositPercentage / 100) * (quotation.MaterialCost + quotation.ConstructionCost):N0} VND
   - Remaining Balance: Payable upon project completion

5. FORCE MAJEURE:
   - Force majeure refers to unforeseen events beyond the control of the parties and cannot be prevented despite all necessary measures.
   - These include natural disasters (storms, floods, earthquakes, fires), war, terrorism, public unrest, strikes, transportation paralysis, government orders or legal changes beyond the control of the parties.
   - In such events, the affected party shall not be liable for breach of contract.
   - The affected party must promptly notify the other party to seek remedies and minimize damage.
   - If the contract becomes impossible to perform, both parties shall negotiate solutions before contract termination.
   - The implementation timeline shall be extended in accordance with the duration and consequences of the force majeure event.

6. RIGHTS AND OBLIGATIONS OF THE PARTIES:
   * Party A:
     - Provide all necessary documents and site access.
     - Coordinate and resolve arising issues during design and construction phases.
     - Make payments to Party B in accordance with the contract.

   * Party B:
     - Ensure progress and timely handover of the project as agreed.
     - Coordinate with Party A and deliver approved services.
     - Provide all tools and equipment for execution.
     - Be responsible for workers’ meals, water, medication, and health.
     - Ensure the quality of provided materials.
     - Maintain occupational safety during construction.
     - Keep confidentiality of all related documents unless approved by Party A.
     - Correct all errors resulting from Party B’s design responsibility.
     - Maintain site cleanliness.
     - If Party A requests design changes after quotation approval, Party A shall bear additional costs accordingly.
     - The modification time is included in the contract period as agreed.
     - Provide warranty for completed work.
     - Assign Mr./Ms. {provider?.LastName} {provider?.FirstName} (Service Representative, Phone: {provider?.Phone}) as the point of contact with Party A.

7. MODIFICATIONS & ADDITIONAL REQUESTS:
   - All new requests must be agreed via the chat system.
   - They will be appended as a contract appendix with cost and time adjustments accordingly.

8. GENERAL TERMS:
   - The contract is effective from the signing date until both parties complete the handover and liquidation.
   - In the event of a dispute, both parties shall negotiate first before considering legal actions.
   - Party B is responsible for providing all necessary documentation related to the project and will be held liable if insufficient.
   - Party A agrees not to use the documents provided by Party B for other purposes outside the scope of this contract.
   - Both parties commit to fully implementing the terms and conditions of this contract.";
        }


        private string GenerateSignatureEmailContent(string contractCode, string token)
        {
            var verifyUrl = $"http://localhost:3000/sign?token={Uri.EscapeDataString(token)}";

            return $@"
📄 CONTRACT: {contractCode}

You have requested to digitally sign your Seasonal Home Decor Service Contract.

👉 Please click the following link to confirm your digital signature:
{verifyUrl}

❗ If you did not make this request, please ignore this email or contact support.

Best regards,  
Home Seasonal Decoration System
";
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

        private byte[] CreatePdf(string termOfUseContent)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Thiết lập font và margin
                var regularFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                document.SetMargins(50, 40, 50, 40);

                // Tiêu đề chính (giữ nguyên như code gốc)
                document.Add(new Paragraph("SEASONAL HOME DECORATION SERVICE CONTRACT")
                    .SetFont(boldFont)
                    .SetFontSize(13)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));

                // Ngày tháng (giữ nguyên)
                document.Add(new Paragraph($"Date: {DateTime.Now:dd/MM/yyyy}")
                   .SetFont(regularFont)
                   .SetFontSize(8)
                   .SetTextAlignment(TextAlignment.RIGHT)
                   .SetMarginBottom(10));

                // Xử lý nội dung với định dạng tốt hơn
                var lines = termOfUseContent.Split('\n');
                bool isServiceDetailsSection = false;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    Paragraph para;
                    float fontSize = 10;
                    int leftIndent = 0;

                    // Tiêu đề chính (1., 2., 3.)
                    if (Regex.IsMatch(line, @"^\d+\.\s[A-Z]"))
                    {
                        // Thêm khoảng trống trước mỗi section mới
                        document.Add(new Paragraph(" ").SetMarginBottom(5));

                        para = new Paragraph(line)
                            .SetFont(boldFont)
                            .SetFontSize(12)
                            .SetMarginBottom(8);

                        // Thêm đường kẻ ngang dưới tiêu đề
                        document.Add(new LineSeparator(new SolidLine(0.5f))
                            .SetMarginBottom(8));
                    }
                    // Mục con trong SERVICE DETAILS
                    else if (isServiceDetailsSection && Regex.IsMatch(line, @"^\s+\d+\."))
                    {
                        para = new Paragraph(line)
                            .SetFont(regularFont)
                            .SetFontSize(fontSize)
                            .SetMarginBottom(4);
                        leftIndent = 15;
                    }
                    // Các bullet points
                    else if (line.StartsWith("   *") || line.StartsWith("   -"))
                    {
                        para = new Paragraph(line.Trim())
                            .SetFont(boldFont)
                            .SetFontSize(fontSize)
                            .SetMarginBottom(4);
                        leftIndent = 10;
                    }
                    // Nội dung thông thường
                    else
                    {
                        para = new Paragraph(line)
                            .SetFont(regularFont)
                            .SetFontSize(fontSize)
                            .SetMarginBottom(6);
                    }

                    // Cập nhật trạng thái section
                    if (line.StartsWith("2. SERVICE DETAILS:"))
                    {
                        isServiceDetailsSection = true;
                    }
                    else if (line.StartsWith("3. IMPLEMENTATION TIME:"))
                    {
                        isServiceDetailsSection = false;
                    }

                    if (leftIndent > 0)
                    {
                        para.SetPaddingLeft(leftIndent);
                    }

                    document.Add(para);
                }

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF Generation Error: {ex}");
                throw;
            }
        }
        #endregion
    }
}
