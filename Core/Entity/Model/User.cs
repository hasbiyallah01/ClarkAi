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
        [Required]
        [Column("name")]
        public string Name { get; set; }
        [Required, EmailAddress]
        [Column("email")]
        public string Email { get; set; }
        [Column("nickname")]
        public string Nickname { get; set; }
        [Required]
        [Column("password")]
        public string Password { get; set; }
        [Required]
        [Column("role")]
        public string Role { get; set; }
        [Column("school")]
        public string School { get; set; }
        [Column("department")]
        public string Department { get; set; }
        [Column("interest")]
        public string Interest { get; set; }

        [Column("streakCount")]
        public int StreakCount { get; set; }
        [Column("paystackAuthorizationCode")]
        public string? PaystackAuthorizationCode { get; set; }
        [Column("paystackCustomerCode")]
        public string? PaystackCustomerCode { get; set; }
        [Column("image_url")]
        public string ImageUrl { get; set; }
        [Column("oauth")]
        public string Oauth { get; set; }
        [Column("study_vibe")]
        public JsonDocument StudyVibe { get; set; }


        [Column("plantype")]
        public PlanType PlanType { get; set; }
        [Column("subcriptionstatus")]
        public SubscriptionStatus SubscriptionStatus { get; set; }


        [Column("nextBillingDate")]
        public DateTime? NextBillingDate { get; set; }
        [Column("lastStreakDate")]
        public DateTime? LastStreakDate { get; set; }
        [Column("createdAt")]
        public DateTime CreatedAt { get; set; }
        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
