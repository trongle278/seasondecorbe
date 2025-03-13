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

                string displayName = account.IsProvider == true && !string.IsNullOrEmpty(account.BusinessName)
                    ? account.BusinessName
                    : $"{account.FirstName} {account.LastName}";

                var lastMessage = chatHistory
                    .Where(chat => chat.SenderId == contact.ContactId || chat.ReceiverId == contact.ContactId)
                    .OrderByDescending(chat => chat.SentTime)
                    .FirstOrDefault();

                return new ContactResponse
                {
                    ContactId = contact.ContactId,
                    ContactName = displayName,
                    Avatar = account.Avatar,
                    Message = lastMessage?.Message,
                    LastMessageTime = lastMessage?.SentTime.ToString("dd/MM/yy")
                };
            })
            .OrderByDescending(c => c.Message != null)
            .ThenByDescending(c => c.LastMessageTime)
            .ToList();

            return new BaseResponse
            {
                Success = true,
                Message = "Contacts retrieved successfully.",
                Errors = new List<string>(),
                Data = contactList
            };
        }

        public async Task<BaseResponse> AddToContactListAsync(int userId, int receiverId)
        {
            if (userId == receiverId)
                return new BaseResponse 
                { 
                    Success = false, 
                    Message = "Cannot add yourself as a contact.", 
                    Errors = new List<string>() 
                };

            var contactExists = await _unitOfWork.ContactRepository.ContactExistsAsync(userId, receiverId);
            
            if (contactExists)
                return new BaseResponse 
                { 
                    Success = false, 
                    Message = "Contact already exists.", 
                    Errors = new List<string>() 
                };

            var newContact = new Contact
            {
                UserId = userId,
                ContactId = receiverId
            };

            await _unitOfWork.ContactRepository.InsertAsync(newContact);
            await _unitOfWork.CommitAsync();

            return new BaseResponse 
            { 
                Success = true,
                Message = "Contact added successfully.", 
                Errors = new List<string>(), 
                Data = null 
            };
        }
    }
}
