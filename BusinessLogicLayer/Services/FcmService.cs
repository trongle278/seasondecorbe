using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace BusinessLogicLayer.Services
{
    public class FcmService
    {
        public FcmService()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "ServiceAccount",
                    "seasondecor-4a83d-firebase-adminsdk-fbsvc-2747aa752e.json"
                );
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPath)
                });
            }
        }

        public async Task<string> SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string> data = null)
        {
            var message = new Message()
            {
                Token = deviceToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body,
                },
                Data = data,
            };

            // Gửi thông báo và trả về response từ FCM
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            return response;
        }
    }
}
