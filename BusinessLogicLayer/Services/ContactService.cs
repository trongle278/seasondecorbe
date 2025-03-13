using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ModelResponse;
using DataAccessObject.Models;
using Repository.UnitOfWork;

namespace BusinessLogicLayer.Services
{
    public class ContactService : IContactService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContactService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse> GetAllContactsAsync(int userId)
        {
            var contacts = await _unitOfWork.ContactRepository.GetUserContactsAsync(userId);
            var chatHistory = await _unitOfWork.ChatRepository.GetUserChatAsync(userId);

            var contactList = contacts.Select(contact =>
            {
                var account = contact.ContactUser;

                // Xác định tên hiển thị
                string displayName = account.IsProvider == true && !string.IsNullOrEmpty(account.BusinessName)
                    ? account.BusinessName
                    : $"{account.FirstName} {account.LastName}";

                // Tìm tin nhắn gần nhất
                var lastMessage = chatHistory
                    .Where(chat => chat.SenderId == contact.ContactId || chat.ReceiverId == contact.ContactId)
                    .OrderByDescending(chat => chat.SentTime)
                    .FirstOrDefault();

                return new ContactResponse
                {
                    ContactId = contact.ContactId,
                    ContactName = displayName,
                    Avatar = account.Avatar, // Trả avatar
                    Message = lastMessage?.Message,
                    LastMessageTime = lastMessage?.SentTime ?? contact.CreatedAt // Nếu chưa có tin nhắn, lấy CreatedAt của Contact
                };
            })
            .OrderByDescending(c => c.LastMessageTime) // Sắp xếp theo SentTime mới nhất
            .ToList();
            
            return new BaseResponse
            {
                Success = true,
                Message = "Contacts retrieved successfully.",
                Data = contactList
            };
        }

        public async Task AddToContactListAsync(int userId, int contactId)
        {
            if (userId == contactId)
                throw new InvalidOperationException("Cannot add yourself as a contact.");

            var contactExists = await _unitOfWork.ContactRepository.ContactExistsAsync(userId, contactId);
            if (contactExists) return;

            var newContact = new Contact
            {
                UserId = userId,
                ContactId = contactId
            };

            await _unitOfWork.ContactRepository.InsertAsync(newContact);
            await _unitOfWork.CommitAsync();
        }
    }
}
