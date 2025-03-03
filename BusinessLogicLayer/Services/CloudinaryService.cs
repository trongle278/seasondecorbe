using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.IO;
using System.Threading.Tasks;

// Alias for Cloudinary's Account
using CloudinaryAccount = CloudinaryDotNet.Account;
using BusinessLogicLayer.Interfaces;

namespace BusinessLogicLayer.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(string cloudName, string apiKey, string apiSecret)
        {
            var account = new CloudinaryAccount(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        /// <summary>
        /// Upload avatar dành riêng cho ảnh, với transform (resize, crop) 
        /// </summary>
        /// <param name="fileStream">Stream file ảnh</param>
        /// <param name="fileName">Tên file (có đuôi .jpg, .png, ...)</param>
        /// <returns>URL ảnh trên Cloudinary</returns>
        public async Task<string> UploadAvatarAsync(Stream fileStream, string fileName)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = $"avatars/{fileName}", // Lưu file vào folder "avatars" 
                Overwrite = true,
                Transformation = new Transformation()
                    .Width(150)
                    .Height(150)
                    .Crop("fill")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl?.ToString();
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType = null)
        {
            // Giả sử ta phân biệt "ảnh" hay "file khác" dựa vào contentType
            // Nếu contentType chứa "image", ta coi là ảnh => ImageUploadParams
            // Ngược lại => RawUploadParams
            bool isImage = !string.IsNullOrEmpty(contentType) && contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase);

            if (isImage)
            {
                // Dùng ImageUploadParams
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    PublicId = fileName,
                    // Bạn có thể thêm Transformation (resize, crop, v.v.) nếu muốn
                    // Transformation = new Transformation().Width(150).Height(150).Crop("fill")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl?.ToString();
            }
            else
            {
                // Dùng RawUploadParams cho tất cả file khác (pdf, docx, txt, xlsx,...)
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    PublicId = fileName
                    // resource_type = "raw" (mặc định, read-only)
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.SecureUrl?.ToString();
            }
        }

        //public async Task DeleteFileAsync(string publicId, bool isImage)
        //{
        //    if (isImage == true)
        //    {
        //        var deletionParams = new DeletionParams(publicId)
        //        {
        //            ResourceType = ResourceType.Image
        //        };

        //        var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

        //        if (deletionResult.Result != "ok")
        //        {
        //            throw new Exception("Error deleting image from Cloudinary.");
        //        }
        //    }
        //    else
        //    {
        //        var deletionParams = new DeletionParams(publicId)
        //        {
        //            ResourceType = ResourceType.Raw
        //        };

        //        var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

        //        if (deletionResult.Result != "ok")
        //        {
        //            throw new Exception("Error deleting raw file from Cloudinary.");
        //        }
        //    }
        //}

        public async Task DeleteImageAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

            if (deletionResult.Result != "ok")
            {
                throw new Exception("Error deleting image from Cloudinary.");
            }
        }

        public async Task DeleteFileAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Raw
            };

            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

            if (deletionResult.Result != "ok")
            {
                throw new Exception("Error deleting raw file from Cloudinary.");
            }
        }
    }
}
