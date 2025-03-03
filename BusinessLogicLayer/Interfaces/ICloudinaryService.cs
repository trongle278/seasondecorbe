using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;

namespace BusinessLogicLayer.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadAvatarAsync(Stream fileStream, string fileName);
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType = null);
        Task DeleteImageAsync(string publicId);
        Task DeleteFileAsync(string publicId);
    }
}
