using ClarkAI.Core.Entity.Model;
using ClarkAI.Models;
using ClarkAI.Models.UserModel;
using ClarkAI.Models.UserModel.LoginModel;
using System.Text.Json.Serialization.Metadata;

namespace ClarkAI.Core.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<BaseResponse<UserResponse>> GetCurrentUserAsync();
        Task<BaseResponse<UserResponse>> GetUser(string email);
        Task<BaseResponse<ICollection<UserResponse>>> GetAllUsers();
        Task<BaseResponse> UpdateUser(int id, UserRequest user);
        Task<BaseResponse<LoginResponse>> Login(LoginRequest login);
        Task<BaseResponse<UserResponse>>  CreateUser(UserRequest user);
    }
}
