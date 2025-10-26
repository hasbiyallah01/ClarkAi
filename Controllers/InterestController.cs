using ClarkAI.Core.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClarkAI.Controllers
{
    [Route("api/interest")]
    [ApiController]
    public class InterestController : ControllerBase
    {
        public readonly IUserContentService _userContent;
        public readonly IPaystackService _paystackService;

        public InterestController(IUserContentService userContent, IPaystackService paystackService)
        {
            _userContent = userContent;
            _paystackService = paystackService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDailyShortReadAsync()
        {
            try
            {
                var userResponse = await _paystackService.GetCurrentUser();
                if (userResponse == null || !userResponse.IsSuccessful)
                    return Unauthorized(new { message = "User not authenticated or not found" });

                var shortReads = await _userContent.GetOrGenerateDailyContentAsync(userResponse.Value);
                return Ok(shortReads);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("getstreak")]
        public async Task<IActionResult> GetStreak()
        {
            try
            {
                var userResponse = await _paystackService.GetCurrentUser();
                if (userResponse == null || userResponse.IsSuccessful)
                    return Unauthorized(new { message = "User not authenticated." });

                int updatedStreak = await _userContent.GetOrIncreaseDailyStreakAsync(userResponse.Value, false);

                return Ok(new
                {
                    userId = userResponse.Value,
                    streakCount = updatedStreak,
                    message = "Streak updated succesfully"
                });
            }
            catch(Exception ex)
            {
                return BadRequest(new {messsage = ex.Message});
            }
        }

        [HttpPost("streak/increment")]
        public async Task<IActionResult> IncrementStreak()
        {
            try
            {
                var user = await _paystackService.GetCurrentUser();
                if (user == null || !user.IsSuccessful)
                    return Unauthorized(new { message = "User not authenticated." });

                int updatedStreak = await _userContent.
                    GetOrIncreaseDailyStreakAsync(user.Value, true);

                return Ok(new
                {
                    userId = user.Value,
                    streakCount = updatedStreak,
                    message = "Streak updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
