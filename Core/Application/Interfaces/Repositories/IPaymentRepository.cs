using ClarkAI.Core.Entity.Model;

namespace ClarkAI.Core.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByReferenceAsync(string reference);
        Task UpdateAsync(Payment payment);
        Task<bool> HasUserPaid(int userId);
        Task<Payment?> GetBySubscriptionCodeAsync(string subCode);
    }
}
