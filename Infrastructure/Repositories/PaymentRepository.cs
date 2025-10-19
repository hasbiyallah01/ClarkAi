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

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public async Task<Payment?> GetByReferenceAsync(string reference)
        {
            return await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Reference == reference);
        }

        public async Task<Payment?> GetBySubscriptionCodeAsync(string subCode)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.SubscriptionCode == subCode);
        }

        public async Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
        }
        public async Task<bool> HasUserPaid(int userId)
        {
            return await _context.Payments.AnyAsync(p => p.UserId == userId && p.Status == Core.Entity.Enum.PaymentStatus.Success);
        }
    }
}
