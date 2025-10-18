using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Entity.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ClarkAI.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ClarkContext _context;

        public UserRepository(ClarkContext context)
        {
            _context = context;
        }

        public async Task<User> GetAsync(Expression<Func<User, bool>> expression)
        {
            return await _context.Set<User>()
                .SingleOrDefaultAsync(expression);
        }

        public async Task<User> GetUser(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            return user;
        }
    }
}
