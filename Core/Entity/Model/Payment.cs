using ClarkAI.Core.Entity.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClarkAI.Core.Entity.Model
{
    [Table("payments")]
    public class Payment
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("userId")]
        public int UserId { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("reference")]
        public string Reference { get; set; }
        [Column("subscriptionCode")]
        public string? SubscriptionCode { get; set; } 
        [Column("amount")]
        public decimal Amount { get; set; }        
        [Column("currency")]
        public string Currency { get; set; }      
        [Column("status")]
        [EnumDataType(typeof(PaymentStatus))]
        public PaymentStatus Status { get; set; }   
        [Column("paymentDate")]
        public DateTime PaymentDate { get; set; }
        [Column("confirmedAt")]
        public DateTime? ConfirmedAt { get; set; }   
        private DateTime? _dateModified { get; set; }

        [Column("dateModified", TypeName = "timestamp with time zone")]
        public DateTime? DateModified
        {
            get => _dateModified;
            set => _dateModified = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null;
        }
        private DateTime? _dateCreated;

        [Column("dateCreated", TypeName = "timestamp with time zone")]
        public DateTime? DateCreated
        {
            get => _dateCreated;
            set => _dateCreated = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null;
        }
    }
}
