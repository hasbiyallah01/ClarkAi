using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using ClarkAI.Core.Application.Interfaces.Services;
using ClarkAI.Core.Entity.Model;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace ClarkAI.Core.Application.Service
{
    public class CloudflareR2Service : ICloudflareR2Service
    {
        private readonly AmazonS3Client _s3Client;
        private readonly string _endpointDomain;
        public CloudflareR2Service(IOptions<CloudflareR2Setting> settings, AmazonS3Client s3Client, string endpointDomain)
        {

            var cfg = settings.Value;
            if(string.IsNullOrEmpty(cfg.Endpoint) || string.IsNullOrEmpty(cfg.AccesskeyId) 
                || string.IsNullOrEmpty(cfg.EndpointDomain) || string.IsNullOrEmpty(cfg.SecretAccessKey) )
            {
                throw new InvalidOperationException("Clofflare R2 configuration is in complete");
            }

            var config = new AmazonS3Config
            {
                ServiceURL = cfg.Endpoint,
                ForcePathStyle = true,
                RegionEndpoint = RegionEndpoint.USEast1,
            };
            _s3Client = new AmazonS3Client(
                cfg.AccesskeyId,
                cfg.SecretAccessKey,
                config
                );
            
            _endpointDomain = cfg.EndpointDomain;
        }
        public Task DaleteFile(string bucketname, string key)
        {
            throw new NotImplementedException();
        }

        public Task<string> UpdateUserProfilePicture(IFormFile newfile, string oldFileUrl, string bucketName = "clarkuser")
        {
            throw new NotImplementedException();
        }

        public async Task<string> UploadUserPictureAsync(string bucketName, string fileName, Stream FileStream, string mimeType)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,
                InputStream = FileStream,
                ContentType = mimeType,
                CannedACL = S3CannedACL.PublicRead,
            };

            await _s3Client.PutObjectAsync(putRequest);

            return $"https://{_endpointDomain}/{bucketName}/user/{fileName}";
        }

        public async Task<string> UploadUserProfilePicture(IFormFile file)
        {
            string bucketName = "clarkusers";
            string originalFileName = file.FileName;
            string sanitizedFileName = Regex.Replace(
                                    $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{originalFileName}",
                                    @"[^a-zA-Z0-9.]", "_"
                                );

            string mimeType = file.ContentType;

            await using var stream = file.OpenReadStream();

            string uploadUrl = await UploadUserPictureAsync(bucketName, sanitizedFileName, stream, mimeType);

            string imageUrl = $"https://{Environment.GetEnvironmentVariable("RS_USERS_IMAGES_DOMAIN")}/{sanitizedFileName}";

            return imageUrl;
        }
    }
}
