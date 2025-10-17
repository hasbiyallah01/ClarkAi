using ClarkAI.Core.Entity.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ClarkAI.Core.Entity.Model
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("nickname")]
        public string Nickname { get; set; }
        [Column("password")]
        public string Password { get; set; }
        public string Role { get; set; }
        public string School { get; set; }
        public string Department { get; set; }
        public string Interest { get; set; }
        public PlanType PlanType { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public string? PaystackAuthorizationCode { get; set; }
        public string? PaystackCustomerCode { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public int StreakCount { get; set; }
        public DateTime? LastStreakDate { get; set; }
        public JsonDocument InterestVibe { get; set; }
        public string ImageUrl { get; set; }
        public string Oauth { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
