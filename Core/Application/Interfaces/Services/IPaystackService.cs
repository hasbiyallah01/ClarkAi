namespace ClarkAI.Core.Application.Interfaces.Services
{
    public interface IPaystackService
    {
        Task<PaymentResponse> InitializePayment(int userId);
        Task<bool> VerifySubscription (string reference);
        Task<bool> CancelSubscriptionAsync(string subscriptionCode);
        Task<string> CreateRecurringSubscriptionAsync(int userId, string authCode);


    }

    public class PaymentResponse
    {
        public string Reference { get; set; }
        public string AuthorizationUrl { get; set; }
    }
}
