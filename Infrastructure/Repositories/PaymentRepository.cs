using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Entity.Model;

namespace ClarkAI.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ClarkContext _context;

        public PaymentRepository(ClarkContext context)
        {
            _context = context;
        }
        public Task<Payment> AddAsync(Payment payment)
        {
            throw new NotImplementedException();
        }

        public Task<Payment> GetByReference(string ReferenceId)
        {
            throw new NotImplementedException();
        }

        public Task<Payment> GetBySubcriptionCode(string subCode)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasUserPaid(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<Payment> UpdateAsync(Payment payment)
        {
            throw new NotImplementedException();
        }
    }
}
