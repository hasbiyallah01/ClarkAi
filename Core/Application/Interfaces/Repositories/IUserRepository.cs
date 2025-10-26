using ClarkAI.Core.Entity.Model;
using ClarkAI.Models;
using System.Linq.Expressions;

namespace ClarkAI.Core.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User> AddAsync(User user);
        Task<bool> ExistAsync(int id);
        Task<bool> ExistAsync(string email, int id);
        Task<bool> ExistAsync(string email);
        Task<User> GetUser(int id);
        Task<User> UpdateAsync(User user);
        Task<User> GetAsync(Expression<Func<User, bool>> expression);
        Task<bool> Exist(int id);
        Task<ICollection<User>> GetAllAsync();
    }
}
