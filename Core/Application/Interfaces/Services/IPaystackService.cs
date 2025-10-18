namespace ClarkAI.Core.Application.Interfaces.Services
{
    public interface IPaystackService
    {
        Task<bool> CancelSubscriptionAsync(string subscriptionCode);

    }
}
