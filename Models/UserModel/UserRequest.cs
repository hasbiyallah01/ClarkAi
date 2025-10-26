using ClarkAI.Core.Entity.Enum;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ClarkAI.Models.UserModel
{
    public class UserRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password doesn't match")]
        public string ConfirmPassword { get; set; }


        public string Nickname { get; set; }

        [Required]
        public string Role { get; set; }

        public string School { get; set; }
        public string Department { get; set; }
        public string Interests { get; set; }
        public JsonDocument StudyVibe {  get; set; }
        public IFormFile ImageFile { get; set; }
        public PlanType PlanType { get; set; } = PlanType.Free;

    }
}
