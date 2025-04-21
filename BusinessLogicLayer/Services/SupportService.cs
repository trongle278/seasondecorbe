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
                var ticket = new Support
                {
                    Subject = request.Subject,
                    Description = request.Description,
                    CreateAt = DateTime.Now,
                    AccountId = accountId,
                    TicketTypeId = request.TicketTypeId,
                    BookingId = request.BookingId,
                    TicketStatus = Support.TicketStatusEnum.Pending,
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

        public async Task<BaseResponse<SupportReplyResponse>> AddReplyAsync(AddSupportReplyRequest request, int supportId, int accountId, bool isAdmin)
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

                // 🔥 Tạo reply
                var reply = new TicketReply
                {
                    Description = request.Description,
                    CreateAt = DateTime.UtcNow,
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
                                UploadedAt = DateTime.UtcNow,
                                SupportId = supportId,     // ✅ truyền SupportId
                                TicketReplyId = reply.Id   // ✅ gán ReplyId vừa tạo
                            };

                            await _unitOfWork.TicketAttachmentRepository.InsertAsync(attachment);
                        }
                    }
                    await _unitOfWork.CommitAsync();
                }

                // 🔥 Update lại ticket
                ticket.TicketStatus = isAdmin ? Support.TicketStatusEnum.Solved : Support.TicketStatusEnum.Pending;
                _unitOfWork.SupportRepository.Update(ticket);
                await _unitOfWork.CommitAsync();

                // 🔥 Map dữ liệu trả về
                var mappedReply = new SupportReplyResponse
                {
                    Id = reply.Id,
                    SupportId = reply.SupportId,
                    AccountId = reply.AccountId,
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
                var ticket = await _unitOfWork.SupportRepository.GetByIdAsync(supportId);
                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Ticket not found.";
                    return response;
                }

                response.Success = true;
                response.Data = _mapper.Map<SupportResponse>(ticket);
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

        public async Task<BaseResponse<PageResult<AdminSupportPaginateResponse>>> GetPaginatedSupportForAdminAsync(SupportFilterRequest request)
        {
            var response = new BaseResponse<PageResult<AdminSupportPaginateResponse>>();
            try
            {
                // 🔹 Filter condition
                Expression<Func<Support, bool>> filter = ticket =>
                    (!request.TicketStatus.HasValue || ticket.TicketStatus == request.TicketStatus.Value) &&
                    //(!request.AccountId.HasValue || ticket.AccountId == request.AccountId.Value) &&
                    (!request.BookingId.HasValue || ticket.BookingId == request.BookingId.Value);

                // 🔹 Order by condition
                Expression<Func<Support, object>> orderByExpression = request.SortBy switch
                {
                    "BookingCode" => ticket => ticket.Booking.BookingCode,
                    "TicketStatus" => ticket => ticket.TicketStatus,
                    _ => ticket => ticket.CreateAt
                };

                // 🔹 Includes (nếu cần)
                Func<IQueryable<Support>, IQueryable<Support>> customQuery = query => query
                    .Include(t => t.Account)
                    .Include(t => t.Booking)
                    .Include(t => t.TicketType);

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
                var ticketResponses = tickets.Select(ticket => new AdminSupportPaginateResponse
                {
                    Id = ticket.Id,
                    Subject = ticket.Subject,
                    Description = ticket.Description,
                    CreateAt = ticket.CreateAt,
                    TicketStatus = (int)ticket.TicketStatus,
                    BookingId = ticket.BookingId,
                    CustomerId = ticket.AccountId,
                    CustomerName = $"{ticket.Account.FirstName} {ticket.Account.LastName}",
                    TicketType = ticket.TicketType.Type,

                    AttachmentUrls = ticket.TicketAttachments?
                        .Where(a => a.SupportId == ticket.Id && a.TicketReplyId == null)
                        .Select(a => a.FileUrl)
                        .ToList() ?? new List<string>()
                }).ToList();

                response.Success = true;
                response.Data = new PageResult<AdminSupportPaginateResponse>
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
                    (!request.TicketStatus.HasValue || ticket.TicketStatus == request.TicketStatus.Value);

                // 🔹 Sắp xếp (mặc định mới nhất trước)
                Expression<Func<Support, object>> orderByExpression = request.SortBy switch
                {
                    "Subject" => ticket => ticket.Subject,
                    "Status" => ticket => ticket.TicketStatus,
                    _ => ticket => ticket.CreateAt
                };

                // 🔹 Includes (nếu cần thiết lấy thêm Booking, TicketType,...)
                Func<IQueryable<Support>, IQueryable<Support>> customQuery = query => query
                    .AsSplitQuery()
                    .Include(t => t.TicketType)
                    .Include(t => t.TicketReplies)
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
                    Subject = ticket.Subject,
                    Description = ticket.Description,
                    CreateAt = ticket.CreateAt,
                    TicketStatus = (int)ticket.TicketStatus,
                    TicketType = ticket.TicketType.Type,

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
    }
}

