using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Application.Interfaces.Services;
using ClarkAI.Core.Entity.Model;
using ClarkAI.Models;
using ClarkAI.Models.UserModel;
using ClarkAI.Models.UserModel.LoginModel;
using System.Security.Claims;

namespace ClarkAI.Core.Application.Service
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContext;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ICloudflareR2Service _cloudflareR2Service;

        public UserService(IHttpContextAccessor httpContext, IUserRepository userRepository, IUnitOfWork unitOfWork, ICloudflareR2Service cloudflareR2Service,
            IConfiguration configuration)
        {
            _httpContext = httpContext;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _cloudflareR2Service = cloudflareR2Service;
            _configuration = configuration;
        }
        public async Task<BaseResponse<UserResponse>> CreateUser(UserRequest request)
        {
            if (await _userRepository.ExistAsync(request.Email))
            {
                return new BaseResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = "",
                };
            }
            if(request.Password != request.ConfirmPassword)
            {
                return new BaseResponse<UserResponse>
                {
                    Message = "",
                    IsSuccessful = false,
                };
            }

            string imageUrl = null;
            if(request.ImageFile != null)
            {
                imageUrl = await _cloudflareR2Service.UploadUserProfilePicture(request.ImageFile);
            }

            var user = new User
            {
                Email = request.Email,
                Name = request.Name,
                Department = request.Department,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                ImageUrl = imageUrl,
                Interests = request.Interests,
                Nickname = request.Nickname,
                Plan = Entity.Enum.PlanType.Free,
                School = request.School,
                Role = request.Role,
                StudyVibe = request.StudyVibe,
                StreakCount = 0,
                SubscriptionStatus = Entity.Enum.SubscriptionStatus.Inactive,
            };

            var newUser = await _userRepository.AddAsync(user);
            await _unitOfWork.SaveAsync();
            return new BaseResponse<UserResponse>
            {
                 IsSuccessful = true,
                  Message = "",
                  Value = new UserResponse
                  {
                      Id = newUser.Id,
                      Department = request.Department,
                      Email = request.Email,
                      ImageUrl = user.ImageUrl,
                      Interests = request.Interests,
                      Name = request.Name,
                      PlanType = request.PlanType,
                      Nickname = request.Nickname,
                      Role = request.Role,
                      School = request.School,
                      StudyVibe = request.StudyVibe
                  }
            };
        }

        public async Task<BaseResponse<ICollection<UserResponse>>> GetAllUsers()
        {
            var users = await _userRepository.GetAllAsync();

            var userResponses = users.Select(user => new UserResponse
            {
                Id = user.Id,
                Department = user.Department,
                Email = user.Email,
                Nickname = user.Nickname,
                ImageUrl = user.ImageUrl,
                Interests = user.Interests,
                Name = user.Name,
                PlanType = user.Plan,
                Oauth = user.Oauth,
                School = user.School,
                Role = user.Role,
                StudyVibe = user.StudyVibe
            }).ToList();

            return new BaseResponse<ICollection<UserResponse>>
            {
                 IsSuccessful = true,
                 Message = "",
                 Value = userResponses
            };
        }

        public Task<BaseResponse<UserResponse>> GetCurrentUserAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<UserResponse>> GetUser(string email)
        {
            var user = await _userRepository.GetAsync(x => x.Email == email);


            return new BaseResponse<UserResponse>
            {
                IsSuccessful = true,
                Message = "",
                Value = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Nickname = user.Nickname,
                    ImageUrl = user.ImageUrl,
                    Interests = user.Interests,
                    Name = user.Name,
                    PlanType = user.Plan,
                    Oauth = user.Oauth,
                    School = user.School,
                    Department = user.Department,
                    StudyVibe = user.StudyVibe,
                    Role = user.Role,
                }
            };
        }

        public async Task<BaseResponse<LoginResponse>> Login(LoginRequest login)
        {
            var user = await _userRepository.GetAsync(x => x.Email == login.Email);
            if(user == null)
            {
                return new BaseResponse<LoginResponse>
                { 
                    IsSuccessful = false,
                    Message = ""
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
            {
                return new BaseResponse<LoginResponse>
                {
                    IsSuccessful = false,
                    Message = ""
                };
            }

            LoginResponse response = new LoginResponse
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role
            };

            return new BaseResponse<LoginResponse>
            {
                Message = "",
                IsSuccessful = true,
                Value = response
            };
        }

        public async Task<BaseResponse> UpdateUser(int id, UserRequest request)
        {
            var user = await _userRepository.GetAsync(x => x.Id == id);

            if (user == null)
            {
                return new BaseResponse
                {
                    Message = "",
                    IsSuccessful = false,
                };
            }

            if (await _userRepository.ExistAsync(request.Email, id))
            {
                return new BaseResponse
                { 
                    IsSuccessful = false,
                    Message = ""
                };
            }

            if (request.Password != user.Password)
            {
                return new BaseResponse
                { 
                    Message = "",
                    IsSuccessful = false,
                };

            }

            var loginUser = _httpContext.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

            var imageUrl = await _cloudflareR2Service.UploadUserProfilePicture(request.ImageFile);

            user.Name = request.Name;
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.Email = request.Email;
            user.School = request.School;
            user.Department = request.Department;
            user.Interests = request.Interests;
            user.Role = request.Role;
            user.ImageUrl = imageUrl;
            user.UpdatedAt = DateTime.UtcNow;
            user.StudyVibe = request.StudyVibe;

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();

            return new BaseResponse
            { 
                IsSuccessful = true,
                Message = ""
            };

        }
    }
}
