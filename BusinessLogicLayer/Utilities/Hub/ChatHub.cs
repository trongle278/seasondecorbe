using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelRequest.Pagination;
using BusinessLogicLayer.ModelResponse;
using BusinessLogicLayer.Services;
using DataAccessObject.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Utilities.Hub
{
    public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private static readonly Dictionary<int, string> _userConnections = new();
        private readonly IChatService _chatService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public ChatHub(IUnitOfWork unitOfWork, IChatService chatService, ICloudinaryService cloudinaryService)
        {
            _chatService = chatService;
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.User == null)
            {
                Console.WriteLine("Context.User is null");
            }
            else
            {
                var userIdClaim = Context.User.FindFirst("nameid");
                if (userIdClaim == null)
                {
                    Console.WriteLine("Claim 'nameid' not found in token.");
                }
                else
                {
                    Console.WriteLine($"UserId: {userIdClaim.Value}");
                    _userConnections[int.Parse(userIdClaim.Value)] = Context.ConnectionId;
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Context.User == null)
            {
                Console.WriteLine("Context.User is null");
            }
            else
            {
                var userIdClaim = Context.User.FindFirst("nameid");
                if (userIdClaim == null)
                {
                    Console.WriteLine("Claim 'nameid' not found in token.");
                }
                else
                {
                    Console.WriteLine($"UserId: {userIdClaim.Value}");
                    _userConnections[int.Parse(userIdClaim.Value)] = Context.ConnectionId;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Gửi tin nhắn + file (base64)
        public async Task SendMessage(int receiverId, string message, List<FileRequest> files)
        {
            if (Context.User != null)
            {
                var senderIdClaim = Context.User.FindFirst("nameid");
                if (senderIdClaim != null && int.TryParse(senderIdClaim.Value, out var senderId))
                {
                    // Tạo entity Chat
                    var chat = new Chat
                    {
                        SenderId = senderId,
                        ReceiverId = receiverId,
                        Message = message,
                        SentTime = DateTime.UtcNow,
                        IsRead = false
                    };

                    // Lưu tin nhắn vào database
                    await _unitOfWork.ChatRepository.InsertAsync(chat);
                    await _unitOfWork.CommitAsync();

                    // Xử lý file đính kèm
                    if (files != null && files.Any())
                    {
                        foreach (var fileRequest in files)
                        {
                            // Chuyển đổi base64 thành IFormFile
                            var formFile = ConvertBase64ToFormFile(fileRequest);

                            // Upload file lên Cloudinary hoặc lưu trữ khác
                            var fileUrl = await _cloudinaryService.UploadFileAsync(formFile.OpenReadStream(), formFile.FileName);

                            // Tạo entity ChatFile
                            var chatFile = new ChatFile
                            {
                                ChatId = chat.Id,
                                FileName = formFile.FileName,
                                FileUrl = fileUrl,
                                UploadedAt = DateTime.UtcNow
                            };

                            // Lưu file vào database
                            await _unitOfWork.ChatFileRepository.InsertAsync(chatFile);
                        }

                        await _unitOfWork.CommitAsync();
                    }

                    // Gửi tin nhắn real-time đến receiver (thằng B)
                    if (_userConnections.TryGetValue(receiverId, out var receiverConn))
                    {
                        await Clients.Client(receiverConn).SendAsync("ReceiveMessage", new ChatMessageResponse
                        {
                            Id = chat.Id,
                            SenderId = chat.SenderId,
                            ReceiverId = chat.ReceiverId,
                            Message = chat.Message,
                            SentTime = chat.SentTime,
                            IsRead = chat.IsRead,
                            Files = chat.ChatFiles.Select(cf => new ChatFileResponse
                            {
                                FileId = cf.Id,
                                FileName = cf.FileName,
                                FileUrl = cf.FileUrl,
                                UploadedAt = cf.UploadedAt
                            }).ToList()
                        });
                    }

                    // Gửi phản hồi cho sender (thằng A)
                    await Clients.Caller.SendAsync("MessageSent", new ChatMessageResponse
                    {
                        Id = chat.Id,
                        SenderId = chat.SenderId,
                        ReceiverId = chat.ReceiverId,
                        Message = chat.Message,
                        SentTime = chat.SentTime,
                        IsRead = chat.IsRead,
                        Files = chat.ChatFiles.Select(cf => new ChatFileResponse
                        {
                            FileId = cf.Id,
                            FileName = cf.FileName,
                            FileUrl = cf.FileUrl,
                            UploadedAt = cf.UploadedAt
                        }).ToList()
                    });
                }
                else
                {
                    throw new InvalidOperationException("Sender ID not found in token.");
                }
            }
            else
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
        }


        // Đánh dấu tin nhắn đã đọc
        public async Task MarkAsRead(int senderId)
        {
            if (Context.User != null)
            {
                var receiverIdClaim = Context.User.FindFirst("nameid"); // Sử dụng claim "nameid"
                if (receiverIdClaim != null && int.TryParse(receiverIdClaim.Value, out var receiverId))
                {
                    await _chatService.MarkMessagesAsReadAsync(receiverId, senderId);

                    // Thông báo realtime cho sender
                    if (_userConnections.TryGetValue(senderId, out var senderConn))
                    {
                        await Clients.Client(senderConn).SendAsync("MessagesRead", receiverId);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Receiver ID not found in token.");
                }
            }
            else
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
        }

        private IFormFile ConvertBase64ToFormFile(FileRequest fileRequest)
        {
            byte[] fileBytes = Convert.FromBase64String(fileRequest.Base64Content);
            var stream = new MemoryStream(fileBytes);

            return new FormFile(stream, 0, fileBytes.Length, fileRequest.FileName, fileRequest.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = fileRequest.ContentType ?? "application/octet-stream"
            };
        }
    }
}
