namespace ClarkAI.Core.Application.Interfaces.Services
{
    public interface ICloudflareR2Service
    {
        Task<string> UploadUserProfilePicture(IFormFile file);
        Task DaleteFile(string bucketname, string key);
        Task<string> UpdateUserProfilePicture(IFormFile newfile, string oldFileUrl, string bucketName = "clarkuser");
    }
}
