using ClarkAI.Core.Entity.Model;
using System.Linq.Expressions;

namespace ClarkAI.Core.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUser(int id);
        Task<User> UpdateAsync(User user);
        Task<User> GetAsync(Expression<Func<User, bool>> expression);
    }
}
