namespace ClarkAI.Core.Application.Interfaces.Services
{
    public interface IUserContentService
    {
        Task<ShortRead> GenerateDailyContentAsync(int userId);
        Task<ShortRead> GetOrGenerateDailyContentAsync(int userId);
        Task<int> GetOrIncreaseDailyStreakAsync(int userId, bool increament);
    }

    public class ShortRead
    {
        public string Interest { get; set; }
        public string Content { get; set; }
    }

}
