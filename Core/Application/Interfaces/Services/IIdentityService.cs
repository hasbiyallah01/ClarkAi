using ClarkAI.Models.UserModel.LoginModel;

namespace ClarkAI.Core.Application.Interfaces.Services
{
    public interface IIdentityService
    {
        string GenerateToken(string key, LoginResponse response);
        bool isTokenValid(string key, string issuer, string token);
    }
}
