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

        public async Task<SupportResponse> CreateTicketAsync(CreateSupportRequest request)
        {
            var response = new SupportResponse();
            try
            {
                var ticket = new Support
                {
                    Subject = request.Subject,
                    Description = request.Description,
                    CreateAt = DateTime.UtcNow,
                    AccountId = request.AccountId,  // -1 if admin
                    TicketTypeId = request.TicketTypeId,
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
                            var attachment = new TicketAttachment
                            {
                                FileName = file.FileName,
                                FileUrl = fileUrl,
                                UploadedAt = DateTime.UtcNow
                            };
                            ticket.TicketAttachments.Add(attachment);
                        }
                    }
                }

                await _unitOfWork.SupportRepository.InsertAsync(ticket);
                await _unitOfWork.CommitAsync();

                response = _mapper.Map<SupportResponse>(ticket);
                response.Success = true;
                response.Message = "Ticket created successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while creating the ticket";
                response.Errors.Add(ex.Message);
            }
            return response;
        }

        public async Task<SupportReplyResponse> AddReplyAsync(AddSupportReplyRequest request, bool isAdmin)
        {
            SupportReplyResponse replyResponse = null;
            try
            {
                var ticket = await _unitOfWork.SupportRepository.GetByIdAsync(request.SupportId);
                if (ticket == null)
                    throw new Exception("Ticket not found");

                var reply = new TicketReply
                {
                    Description = request.Description,
                    CreateAt = DateTime.UtcNow,
                    SupportId = request.SupportId,
                    AccountId = request.AccountId,
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
                                UploadedAt = DateTime.UtcNow,
                                TicketReplyId = reply.Id
                            };
                            reply.TicketAttachments.Add(attachment);
                        }
                    }
                }

                if (ticket.TicketReplies == null)
                    ticket.TicketReplies = new List<TicketReply>();
                ticket.TicketReplies.Add(reply);

                ticket.TicketStatus = isAdmin ? Support.TicketStatusEnum.Solved : Support.TicketStatusEnum.Pending;

                await _unitOfWork.CommitAsync();
                replyResponse = _mapper.Map<SupportReplyResponse>(reply);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while adding the reply: " + ex.Message);
            }
            return replyResponse;
        }

        public async Task<SupportResponse> GetTicketByIdAsync(int id)
        {
            var response = new SupportResponse();
            try
            {
                var ticket = await _unitOfWork.SupportRepository.GetByIdAsync(id);
                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Ticket not found";
                    return response;
                }
                response = _mapper.Map<SupportResponse>(ticket);
                response.Success = true;
                response.Message = "Ticket retrieved successfully";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An error occurred while retrieving the ticket";
                response.Errors.Add(ex.Message);
            }
            return response;
        }
    }
}

