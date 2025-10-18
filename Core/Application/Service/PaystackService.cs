using ClarkAI.Core.Application.Interfaces.Services;

namespace ClarkAI.Core.Application.Service
{
    public class PaystackService : IPaystackService
    {
        public Task<bool> CancelSubscriptionAsync(string subscriptionCode)
        {
            throw new NotImplementedException();
        }
    }
}
