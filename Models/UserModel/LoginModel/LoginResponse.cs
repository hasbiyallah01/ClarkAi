namespace ClarkAI.Models.UserModel.LoginModel
{
    public class LoginResponseModel : LoginResponse
    {
        public string Token { get; set; }
        public UserResponse Data { get; set; }
    }

    public class LoginResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
