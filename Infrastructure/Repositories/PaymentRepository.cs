using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Entity.Model;
using Microsoft.EntityFrameworkCore;

namespace ClarkAI.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ClarkContext _context;

        public PaymentRepository(ClarkContext context)
        {
            _context = context;
        }
        public async Task<Payment> AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            return payment;
        }

        public async Task<Payment> GetByReference(string ReferenceId)
        {
            return await _context.Payments.FirstOrDefaultAsync(a => a.Reference == ReferenceId);
        }

        public async Task<Payment> GetBySubcriptionCode(string subCode)
        {
            return await _context.Payments.FirstOrDefaultAsync(a => a.SubscriptionCode == subCode);
        }

        public async Task<bool> HasUserPaid(int userId)
        {
            return await _context.Payments.AnyAsync(p => p.UserId == userId && p.Status == Core.Entity.Enum.PaymentStatus.Success);
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            return payment;
        }
    }
}
