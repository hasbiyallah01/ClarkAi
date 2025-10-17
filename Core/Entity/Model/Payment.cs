using ClarkAI.Core.Entity.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClarkAI.Core.Entity.Model
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Reference { get; set; }
        public string? SubscriptionCode { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        private DateTime? _dateModified { get; set; }

        [Column("dateModified", TypeName = "timestamp with time zone")]
        public DateTime? DateModified
        {
            get => _dateModified;
            set => _dateModified = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null;
        }

        private DateTime? _dateCreated { get; set; }
        [Column("dateCreated", TypeName = "timestamp with time zone")]
        public DateTime? DateCreated
        {
            get => _dateCreated;
            set => _dateCreated = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null;
        }
    }
}
