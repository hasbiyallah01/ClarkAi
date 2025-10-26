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

        [Column("interests")]
        public string Interests { get; set; }

        [Column("study_vibe")]
        public JsonDocument StudyVibe { get; set; }

        [Column("image_url")]
        public string ImageUrl { get; set; }

        [Column("oauth")]
        public string Oauth { get; set; }


        [Column("plan")]
        [EnumDataType(typeof(PlanType))]
        public PlanType Plan { get; set; } = PlanType.Free;

        [Column("subscriptionstatus")]
        [EnumDataType(typeof(SubscriptionStatus))]
        public SubscriptionStatus? SubscriptionStatus { get; set; }


        
        
        [Column("paystackcustomercode")]
        public string? PaystackCustomerCode { get; set; }
        [Column("paystackauthorizationcode")]
        public string? PaystackAuthorizationCode { get; set; }
        [Column("streakCount")]
        public int StreakCount { get; set; } = 0;

        [Required]
        [Column("createdAt")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        [Column("nextbillingdate")]
        public DateTime? NextBillingDate { get; set; }
        [Column("lastStreakDate")]
        public DateTime? LastStreakDate { get; set; }

    }

    public class PaystackSettings
    {
        public string SecretKey { get; set; }        
        public string PublicKey { get; set; }       
        public string BaseUrl { get; set; }         
        public string PlanCode { get; set; }        
        public string WebhookSecret { get; set; }
    }

    public class CloudflareR2Setting
    {
        public string Endpoint { get; set; }
        public string AccesskeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string EndpointDomain { get; set; }
    }
}
