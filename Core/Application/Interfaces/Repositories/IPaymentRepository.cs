using ClarkAI.Core.Entity.Model;

namespace ClarkAI.Core.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment> AddAsync(Payment payment);
        Task<Payment> GetByReference(string ReferenceId);
        Task<Payment> UpdateAsync(Payment payment);
        Task<bool> HasUserPaid(int userId);
        Task<Payment> GetBySubcriptionCode(string subCode);
    }
}
