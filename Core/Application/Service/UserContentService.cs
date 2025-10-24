using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace ClarkAI.Core.Application.Service
{
    public class UserContentService : IUserContentService
    {
        public readonly IUserRepository _userRepository;
        private readonly IMemoryCache _cache;
        private readonly GeminiClient _client;
        public UserContentService(IUserRepository userRepository, GeminiClient client, IMemoryCache cache)
        {
            _userRepository = userRepository;
            _client = client;
            _cache = cache;
        }

        public async Task<ShortRead> GenerateDailyContentAsync(int userId)
        {
            var user = await _userRepository.GetUser(userId)
                ?? throw new Exception($"User with ID {userId} not found");

            if (string.IsNullOrWhiteSpace(user.Interests))
                user.Interests = "general Knowledge";

            var cachekey = $"shortreads: {userId}:{DateTime.UtcNow:yyyyy-MM-dd}";

            if (_cache.TryGetValue(cachekey, out ShortRead cachedShortRead))
                return cachedShortRead;

            var interests = user.Interests
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var random = new Random();
            var interest = interests[random.Next(interests.Count)];

            var content = await GenerateContentAsync(interest, user.Department);

            var shortRead = new ShortRead
            {
                Interest = interest,
                Content = content
            };

            _cache.Set(cachekey, shortRead, TimeSpan.FromHours(24));
            return shortRead;
        }

        private async Task<string> GenerateContentAsync(string department, string interest)
        {
            string depPart = !string.IsNullOrEmpty(department) ? $"{department} student" : "student";
            string prompt = $"Generate a unique, fun, actionable, practical 1-sentence fun fact for a {depPart} interested in {interest}. Make it clear, fresh, friendly and under 50 words.";

            try
            {
                var result = await _client.GenerateContentAsync(prompt);
                return !string.IsNullOrWhiteSpace(result) ? result.Trim() : $"No generated fun fact for {interest}";
            }
            catch(Exception ex)
            {
                return $"Error generating fun fact for '{interest}' : { ex.Message }" ;
            }
        }

        public async Task<ShortRead> GetOrGenerateDailyContentAsync(int userId)
        {
            var cacheKey = $"shortreads:{userId}:{DateTime.UtcNow:yyyy-MM-dd}";
            if(_cache.TryGetValue(cacheKey, out ShortRead cachedShortRead))
            {
                return cachedShortRead;
            }
            return await GenerateDailyContentAsync(userId);
        }

        public async Task<int> GetOrIncreaseDailyStreakAsync(int userId, bool increament = true)
        {
            var user = await _userRepository.GetUser(userId) ?? throw new Exception($"User with {userId} id not found");

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            if(!increament)
            {
                return user.StreakCount;
            }

            if(!user.LastStreakDate.HasValue)
            {
                user.StreakCount = 1;
            }

            else
            {
                var lastDate = user.LastStreakDate.Value.Date;
                if(lastDate == today)
                {
                    return user.StreakCount;
                }
                else if(lastDate == yesterday)
                {
                    user.StreakCount += 1;
                }
                else
                {
                    user.StreakCount = 1;
                }
            }
            user.LastStreakDate = today;
            await _userRepository.UpdateAsync(user);

            return user.StreakCount;
        }
    }
}
