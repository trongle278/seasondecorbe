using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
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
                    CreateAt = DateTime.UtcNow,
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
                                UploadedAt = DateTime.UtcNow
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

        public async Task<BaseResponse<SupportReplyResponse>> AddReplyAsync(AddSupportReplyRequest request, int accountId, bool isAdmin)
        {
            var response = new BaseResponse<SupportReplyResponse>();
            try
            {
                var ticket = await _unitOfWork.SupportRepository.GetByIdAsync(request.SupportId);
                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Ticket not found.";
                    return response;
                }

                var reply = new TicketReply
                {
                    Description = request.Description,
                    CreateAt = DateTime.UtcNow,
                    SupportId = request.SupportId,
                    AccountId = accountId,
                    TicketAttachments = new List<TicketAttachment>()
                };

                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var file in request.Attachments)
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            string fileUrl = await _cloudinaryService.UploadFileAsync(stream, file.FileName, file.ContentType);
                            var attachment = new TicketAttachment
                            {
                                FileName = file.FileName,
                                FileUrl = fileUrl,
                                UploadedAt = DateTime.UtcNow
                            };
                            reply.TicketAttachments.Add(attachment);
                        }
                    }
                }

                ticket.TicketReplies ??= new List<TicketReply>();
                ticket.TicketReplies.Add(reply);
                ticket.TicketStatus = isAdmin ? Support.TicketStatusEnum.Solved : Support.TicketStatusEnum.Pending;

                await _unitOfWork.CommitAsync();

                var mappedReply = new SupportReplyResponse
                {
                    Id = reply.Id,
                    SupportId = reply.SupportId,
                    AccountId = reply.AccountId,
                    Description = reply.Description,
                    CreateAt = reply.CreateAt,
                    AttachmentUrls = reply.TicketAttachments.Select(a => a.FileUrl).ToList()
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
        public async Task<BaseResponse<SupportResponse>> GetTicketByIdAsync(int id)
        {
            var response = new BaseResponse<SupportResponse>();
            try
            {
                var ticket = await _unitOfWork.SupportRepository.GetByIdAsync(id);
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
        public async Task<BaseResponse<List<SupportResponse>>> GetAllTicketsAsync(int? bookingId, int? accountId)
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
    }
}

