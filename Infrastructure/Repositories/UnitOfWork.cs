using ClarkAI.Core.Application.Interfaces.Repositories;

namespace ClarkAI.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ClarkContext _context;
        public UnitOfWork(ClarkContext context)
        {
            _context = context;
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
