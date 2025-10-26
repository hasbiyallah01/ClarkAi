using ClarkAI.Core.Entity.Enum;
using System.Text.Json;

namespace ClarkAI.Models.UserModel
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Nickname { get; set; }
        public string Role { get; set; }
        public string School { get; set; }
        public string Department { get; set; }
        public string Interests { get; set; }
        public JsonDocument StudyVibe { get; set; }
        public string ImageUrl { get; set; }
        public PlanType PlanType { get; set; }
        public string Oauth { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set;}
    }

}
