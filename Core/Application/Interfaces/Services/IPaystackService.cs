using ClarkAI.Core.Entity.Model;

namespace ClarkAI.Core.Application.Interfaces.Services
{
    public interface IPaystackService
    {
        Task<PaymentResponse> InitializePayment(int userId);
        Task<bool> VerifySubscriptionAsync(string reference);
        Task<bool> CancelSubscriptionAsync(string subscriptionCode);
        Task<string> CreateRecurringSubscriptionAsync(int userId, string authCode);
        Task<BaseResponse<int>> GetCurrentUser();
    }

    public class PaymentResponse
    {
        public string Reference { get; set; }
        public string AuthorizationUrl { get; set; }
    }
}
