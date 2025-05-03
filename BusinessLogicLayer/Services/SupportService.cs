using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.ModelResponse.Pagination;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class SupportService : ISupportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;

        public SupportService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
        }

        public async Task<BaseResponse<SupportResponse>> CreateTicketAsync(CreateSupportRequest request, int accountId)
        {
            var response = new BaseResponse<SupportResponse>();
            try
            {
                // Tìm kiếm Booking bằng BookingCode thay vì BookingId
                var booking = await _unitOfWork.BookingRepository
                    .Queryable()
                    .FirstOrDefaultAsync(b => b.BookingCode == request.BookingCode);

                if (booking == null)
                {
                    response.Success = false;
                    response.Message = "Booking not found.";
                    return response;
                }

                var ticket = new Support
                {
                    Subject = request.Subject,
                    Description = request.Description,
                    CreateAt = DateTime.Now,
                    AccountId = accountId,
                    TicketTypeId = request.TicketTypeId,
                    BookingId = booking.Id,  // Đặt BookingId vào ticket
                    IsSolved = false,
                    TicketAttachments = new List<TicketAttachment>()
                };

                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var file in request.Attachments)
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            string fileUrl = await _cloudinaryService.UploadFileAsync(stream, file.FileName, file.ContentType);
                            ticket.TicketAttachments.Add(new TicketAttachment
                            {
                                FileName = file.FileName,
                                FileUrl = fileUrl,
                                UploadedAt = DateTime.Now
                            });
                        }
                    }
                }

                await _unitOfWork.SupportRepository.InsertAsync(ticket);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Data = _mapper.Map<SupportResponse>(ticket);
                response.Message = "Ticket created successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while creating the ticket.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<SupportReplyResponse>> AddReplyAsync(AddSupportReplyRequest request, int supportId, int accountId)
        {
            var response = new BaseResponse<SupportReplyResponse>();
            try
            {
                // 🔥 Lấy ticket
                var ticket = await _unitOfWork.SupportRepository.GetByIdAsync(supportId);
                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Ticket not found.";
                    return response;
                }

                // 🔒 Nếu ticket đã được mark solved thì không cho reply nữa
                if (ticket.IsSolved == true)
                {
                    response.Success = false;
                    response.Message = "This support ticket has already been marked as solved. You cannot reply to it.";
                    return response;
                }

                // 🔥 Tạo reply
                var reply = new TicketReply
                {
                    Description = request.Description,
                    CreateAt = DateTime.Now,
                    SupportId = supportId, // ✅ SupportId truyền từ ngoài vào
                    AccountId = accountId
                };

                await _unitOfWork.TicketReplyRepository.InsertAsync(reply);
                await _unitOfWork.CommitAsync(); // Để có ReplyId

                // 🔥 Upload file đính kèm
                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var file in request.Attachments)
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            var fileUrl = await _cloudinaryService.UploadFileAsync(stream, file.FileName, file.ContentType);

                            var attachment = new TicketAttachment
                            {
                                FileName = file.FileName,
                                FileUrl = fileUrl,
                                UploadedAt = DateTime.Now,
                                SupportId = supportId,     // ✅ truyền SupportId
                                TicketReplyId = reply.Id   // ✅ gán ReplyId vừa tạo
                            };

                            await _unitOfWork.TicketAttachmentRepository.InsertAsync(attachment);
                        }
                    }
                    await _unitOfWork.CommitAsync();
                }

                _unitOfWork.SupportRepository.Update(ticket);
                await _unitOfWork.CommitAsync();

                // 🔥 Map dữ liệu trả về
                var mappedReply = new SupportReplyResponse
                {
                    Id = reply.Id,
                    Description = reply.Description,
                    CreateAt = reply.CreateAt,
                    AttachmentUrls = request.Attachments?.Select(a => a.FileName).ToList() ?? new List<string>()
                };

                response.Success = true;
                response.Data = mappedReply;
                response.Message = "Reply added successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while adding the reply.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        /// <summary>
        /// Lấy ticket theo ID
        /// </summary>
        public async Task<BaseResponse<SupportResponse>> GetSupportByIdAsync(int supportId)
        {
            var response = new BaseResponse<SupportResponse>();
            try
            {
                var ticket = await _unitOfWork.SupportRepository
                    .Queryable()
                    .Include(t => t.TicketReplies)
                        .ThenInclude(r => r.Account)
                    .Include(t => t.TicketReplies)
                        .ThenInclude(r => r.TicketAttachments)
                    .Include(t => t.Booking)
                    .FirstOrDefaultAsync(t => t.Id == supportId);

                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Ticket not found.";
                    return response;
                }

                var ticketResponse = _mapper.Map<SupportResponse>(ticket);

                ticketResponse.Replies = ticket.TicketReplies
                    .OrderBy(r => r.CreateAt)
                    .Select(r => new SupportReplyResponse
                    {
                        Id = r.Id,
                        Description = r.Description,
                        CreateAt = r.CreateAt,
                        AccountName = $"{r.Account.FirstName} {r.Account.LastName}",
                        AttachmentUrls = r.TicketAttachments?
                            .Select(a => a.FileUrl)
                            .ToList() ?? new List<string>()
                    }).ToList();

                response.Success = true;
                response.Data = ticketResponse;
                response.Message = "Ticket retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving the ticket.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }


        /// <summary>
        /// Lấy danh sách ticket (lọc theo user hoặc booking)
        /// </summary>
        public async Task<BaseResponse<List<SupportResponse>>> GetAllTicketsByRoleUser(int? bookingId, int? accountId)
        {
            var response = new BaseResponse<List<SupportResponse>>();
            try
            {
                var tickets = await _unitOfWork.SupportRepository.GetAllAsync();

                if (bookingId.HasValue)
                    tickets = tickets.Where(t => t.BookingId == bookingId.Value).ToList();

                if (accountId.HasValue)
                    tickets = tickets.Where(t => t.AccountId == accountId.Value).ToList();

                response.Success = true;
                response.Data = _mapper.Map<List<SupportResponse>>(tickets);
                response.Message = "Tickets retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving tickets.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<PageResult<ProviderSupportPaginateResponse>>> GetPaginatedSupportForProviderAsync(SupportFilterRequest request, int accountId)
        {
            var response = new BaseResponse<PageResult<ProviderSupportPaginateResponse>>();
            try
            {
                // 🔹 Filter condition
                Expression<Func<Support, bool>> filter = ticket =>
                    ticket.Booking.DecorService.AccountId == accountId&&
                    (!request.TicketTypeId.HasValue || ticket.TicketTypeId == request.TicketTypeId.Value) &&
                    (!request.IsSolved.HasValue || ticket.IsSolved == request.IsSolved.Value) &&
                    (string.IsNullOrEmpty(request.BookingCode) || ticket.Booking.BookingCode.Contains(request.BookingCode));

                // 🔹 Order by condition
                Expression<Func<Support, object>> orderByExpression = request.SortBy switch
                {
                    "BookingCode" => ticket => ticket.Booking.BookingCode,
                    "TicketTypeId" => ticket => ticket.TicketTypeId,
                    "IsSolved" => ticket => ticket.IsSolved,
                    _ => ticket => ticket.CreateAt
                };

                // 🔹 Includes (nếu cần)
                Func<IQueryable<Support>, IQueryable<Support>> customQuery = query => query
                    .Include(t => t.Account)
                    .Include(t => t.Booking)
                    .Include(t => t.TicketType)
                    .Include(t => t.TicketReplies)  // Thêm phần này để lấy replies
                        .ThenInclude(r => r.Account) // Bao gồm thông tin Account cho replies
                    .Include(t => t.TicketAttachments);  // Bao gồm thông tin Attachments nếu có

                // 🔹 Lấy dữ liệu phân trang
                (IEnumerable<Support> tickets, int totalCount) = await _unitOfWork.SupportRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                // 🔹 Map thủ công
                var ticketResponses = tickets.Select(ticket => new ProviderSupportPaginateResponse
                {
                    Id = ticket.Id,
                    BookingCode = ticket.Booking.BookingCode,
                    Subject = ticket.Subject,
                    Description = ticket.Description,
                    CreateAt = ticket.CreateAt,
                    IsSolved = ticket.IsSolved,
                    CustomerId = ticket.AccountId,
                    CustomerName = $"{ticket.Account.FirstName} {ticket.Account.LastName}",
                    TicketType = ticket.TicketType.Type,

                    // Thêm các reply vào trong response
                    Replies = ticket.TicketReplies
                        .OrderBy(r => r.CreateAt)
                        .Select(r => new SupportReplyResponse
                        {
                            Id = r.Id,
                            Description = r.Description,
                            CreateAt = r.CreateAt,
                            AccountName = $"{r.Account.FirstName} {r.Account.LastName}",
                            AttachmentUrls = r.TicketAttachments?
                                .Select(a => a.FileUrl)
                                .ToList() ?? new List<string>()
                        }).ToList(),

                    // AttachmentUrls (vẫn giữ nguyên)
                    AttachmentUrls = ticket.TicketAttachments?
                        .Where(a => a.SupportId == ticket.Id && a.TicketReplyId == null)
                        .Select(a => a.FileUrl)
                        .ToList() ?? new List<string>()
                }).ToList();

                response.Success = true;
                response.Data = new PageResult<ProviderSupportPaginateResponse>
                {
                    Data = ticketResponses,
                    TotalCount = totalCount
                };
                response.Message = "Support tickets retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving support tickets.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }


        public async Task<BaseResponse<PageResult<SupportResponse>>> GetPaginatedTicketsForCustomerAsync(SupportFilterRequest request, int accountId)
        {
            var response = new BaseResponse<PageResult<SupportResponse>>();
            try
            {
                // 🔹 Filter tickets theo AccountId của Customer
                Expression<Func<Support, bool>> filter = ticket =>
                    ticket.AccountId == accountId &&
                    (!request.TicketTypeId.HasValue || ticket.TicketTypeId == request.TicketTypeId.Value) &&
                    (!request.IsSolved.HasValue || ticket.IsSolved == request.IsSolved.Value) &&
                    (string.IsNullOrEmpty(request.BookingCode) || ticket.Booking.BookingCode.Contains(request.BookingCode));

                // 🔹 Sắp xếp (mặc định mới nhất trước)
                Expression<Func<Support, object>> orderByExpression = request.SortBy switch
                {
                    "Subject" => ticket => ticket.Subject,
                    "TicketTypeId" => ticket => ticket.TicketTypeId,
                    "IsSolved" => ticket => ticket.IsSolved,
                    _ => ticket => ticket.CreateAt
                };

                // 🔹 Includes (nếu cần thiết lấy thêm Booking, TicketType,...)
                Func<IQueryable<Support>, IQueryable<Support>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(t => t.Booking)
                    .Include(t => t.TicketType)
                    .Include(t => t.TicketReplies)  // Bao gồm TicketReplies
                        .ThenInclude(r => r.Account) // Bao gồm thông tin Account cho mỗi reply
                    .Include(t => t.TicketAttachments);

                // 🔹 Get paginated result
                (IEnumerable<Support> tickets, int totalCount) = await _unitOfWork.SupportRepository.GetPagedAndFilteredAsync(
                    filter,
                    request.PageIndex,
                    request.PageSize,
                    orderByExpression,
                    request.Descending,
                    null,
                    customQuery
                );

                // 🔹 Map về DTO
                var ticketResponses = tickets.Select(ticket => new SupportResponse
                {
                    Id = ticket.Id,
                    BookingCode = ticket.Booking.BookingCode,
                    Subject = ticket.Subject,
                    Description = ticket.Description,
                    CreateAt = ticket.CreateAt,
                    IsSolved = ticket.IsSolved,
                    TicketType = ticket.TicketType.Type,

                    // Thêm các reply vào trong response
                    Replies = ticket.TicketReplies
                        .OrderBy(r => r.CreateAt)
                        .Select(r => new SupportReplyResponse
                        {
                            Id = r.Id,
                            Description = r.Description,
                            CreateAt = r.CreateAt,
                            AccountName = $"{r.Account.FirstName} {r.Account.LastName}",
                            AttachmentUrls = r.TicketAttachments?
                                .Select(a => a.FileUrl)
                                .ToList() ?? new List<string>()
                        }).ToList(),

                    // AttachmentUrls (vẫn giữ nguyên)
                    AttachmentUrls = ticket.TicketAttachments?
                        .Where(a => a.SupportId == ticket.Id && a.TicketReplyId == null)
                        .Select(a => a.FileUrl)
                        .ToList() ?? new List<string>()
                }).ToList();

                response.Success = true;
                response.Data = new PageResult<SupportResponse>
                {
                    Data = ticketResponses,
                    TotalCount = totalCount
                };
                response.Message = "Tickets retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error retrieving tickets.";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<BaseResponse<string>> MarkTicketAsSolvedAsync(int supportId)
        {
            var response = new BaseResponse<string>();
            try
            {
                // 🔥 Lấy ticket từ supportId
                var ticket = await _unitOfWork.SupportRepository.GetByIdAsync(supportId);
                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Ticket not found.";
                    return response;
                }

                // 🔥 Đánh dấu ticket là đã giải quyết
                ticket.IsSolved = true;
                _unitOfWork.SupportRepository.Update(ticket);
                await _unitOfWork.CommitAsync();

                response.Success = true;
                response.Message = "Ticket marked as solved successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while marking the ticket as solved.";
                response.Errors.Add(ex.Message);
            }

            return response;
        }
    }
}

