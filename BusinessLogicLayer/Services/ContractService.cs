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
                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .Include(c => c.Quotation)
                    .ThenInclude(q => q.Booking)
                    .FirstOrDefaultAsync(c => c.SignatureToken == signatureToken);

                var booking = contract.Quotation?.Booking;
                

                if (contract == null)
                {
                    response.Message = "Invalid or expired signature token.";
                    return response;
                }

                if (contract.Status != Contract.ContractStatus.Pending)
                {
                    response.Message = "This contract has already been processed.";
                    return response;
                }

                // Optional: kiểm tra thời hạn của token nếu cần
                if (contract.SignatureTokenGeneratedAt.HasValue &&
                    (DateTime.Now - contract.SignatureTokenGeneratedAt.Value).TotalHours > 24)
                {
                    response.Message = "Signature token has expired.";
                    return response;
                }

                // Cập nhật trạng thái là đã ký
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
                var quotation = await _unitOfWork.QuotationRepository
                    .Queryable()
                    .FirstOrDefaultAsync(q => q.QuotationCode == quotationCode);

                if (quotation == null)
                {
                    response.Message = "Quotation not found.";
                    return response;
                }

                var contract = await _unitOfWork.ContractRepository
                    .Queryable()
                    .FirstOrDefaultAsync(c => c.QuotationId == quotation.Id);

                if (contract == null)
                {
                    response.Message = "Contract not found for this quotation.";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(contract.ContractFilePath))
                {
                    response.Message = "No contract file has been uploaded.";
                    return response;
                }

                response.Success = true;
                response.Message = "Contract file URL retrieved successfully.";
                response.Data = new ContractFileResponse
                {
                    ContractCode = contract.ContractCode,
                    Status = (int)contract.Status,
                    IsSigned = contract.isSigned,
                    FileUrl = contract.ContractFilePath
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
1. PARTY INFORMATION
   - Party A (Customer):
     + Full Name: {customer.LastName} {customer.FirstName}
     + Email: {customer.Email}
     + Phone Number: {customer.Phone}


   - Party B (Service Provider):
     + Service Name: {decorService?.Style}
     + Responsible Person: {provider?.LastName} {provider?.FirstName}
     + Contact Email: {provider?.Email}
     + Phone Number: {provider?.Phone}

After agreeing on the quotation, the two parties enter into a construction contract with the following terms:

2. SERVICE DETAILS
   - Party B shall provide decoration services as requested by Party A at the address: {quotation.Booking.Address}
   * The scope of work includes (from quotation):
   {string.Join("\n", quotation.LaborDetails.Select((t, i) =>
    $"     {i + 1}. {t.TaskName} - Unit: {t.Unit}" +
    (t.Area.HasValue ? $" - Area: {t.Area} m²" : "") +
    $" - Cost: {t.Cost:N0} VND"))}

   * The materials to be used in the project include:
    {string.Join("\n", quotation.MaterialDetails.Select((m, i) =>
    $"     {i + 1}. {m.MaterialName} - Quantity: {m.Quantity} - Category: {m.Category} - Unit Cost: {m.Cost:N0} VND"))}

3. IMPLEMENTATION TIME
   - Start Date: {quotation.Booking.ConstructionDate:dd/MM/yyyy}
   - Expected Completion according to the request of customer: {quotation.Booking.ExpectedCompletion}

4. COST AND PAYMENT
   - Total Cost: {(quotation.MaterialCost + quotation.ConstructionCost):N0} VND
   - Deposit ({quotation.DepositPercentage}%): {(quotation.DepositPercentage / 100) * (quotation.MaterialCost + quotation.ConstructionCost):N0} VND
   - Final Payment: Remaining balance upon project completion
   {(quotation.Booking.AdditionalCost.HasValue ? $"- Additional Charges: {quotation.Booking.AdditionalCost:N0} VND (based on extra requests)" : "")}

5. Responsibilities of the Parties
   * Responsibilities of Party A:
   - Provide necessary information, make payments on time, and cooperate during the construction process.

   * Responsibilities of Party B:
   - Ensure the construction progress and handover as requested by Party A.
   - Prepare necessary tools and equipment for the job.
   - Take care of their own meals, water, medicine, and workers’ health.
   - Ensure occupational safety during construction.
   - Maintain overall hygiene.
   - Provide warranty for the completed work. The warranty includes fixing or repairing abnormal errors caused by Party B.

6. MODIFICATIONS & ADDITIONAL REQUESTS

   - New requests must be agreed via chat
   - They will be listed as appendix with cost and time impact

7. GENERAL TERMS
   - This contract is effective from the date of signing.
   - Both parties commit to fully complying with the contract terms.
   - Any disputes will be resolved through negotiation, or legal proceedings if necessary.";}
        
        private string GenerateSignatureEmailContent(string contractCode, string token)
        {
            var verifyUrl = $"https://example.com/verify-signature?token={Uri.EscapeDataString(token)}";

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
            return $"CON{DateTime.Now:yyyyMMddHHmmssfff}";
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

                var writerProperties = new WriterProperties();
                using var writer = new PdfWriter(memoryStream, writerProperties);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                var regularFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);

                // Tiêu đề hợp đồng (Căn giữa)
                document.Add(new Paragraph("SEASONAL HOME DECORATION SERVICE CONTRACT")
                    .SetFont(boldFont)
                    .SetFontSize(13)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));

                // Ngày ký (Căn phải)
                document.Add(new Paragraph($"Date: {DateTime.Now:dd/MM/yyyy}")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(10));

                // Phân tích nội dung hợp đồng theo từng dòng
                var lines = termOfUseContent.Split('\n');

                foreach (var rawLine in lines)
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    Paragraph para;

                    if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") ||
                        line.StartsWith("4.") || line.StartsWith("5.") || line.StartsWith("6.") || line.StartsWith("7."))
                    {
                        para = new Paragraph(line)
                            .SetFont(boldFont)
                            .SetFontSize(10)
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetMarginTop(7)
                            .SetMarginBottom(3);
                    }
                    else if (line.StartsWith("-") || line.StartsWith("+") || line.StartsWith("•"))
                    {
                        para = new Paragraph(line)
                            .SetFont(regularFont)
                            .SetFontSize(10)
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetMarginBottom(2);
                    }
                    else
                    {
                        para = new Paragraph(line)
                            .SetFont(regularFont)
                            .SetFontSize(10)
                            .SetTextAlignment(TextAlignment.JUSTIFIED)
                            .SetMarginBottom(4);
                    }

                    document.Add(para);
                }

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF Generation Error: {ex}");
                throw new Exception("Failed to generate PDF document", ex);
            }
        }
        #endregion
    }
}
