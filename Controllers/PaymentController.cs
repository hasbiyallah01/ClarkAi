using ClarkAI.Core.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClarkAI.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaystackService _paystackService;

        public PaymentController(IPaystackService paystackService)
        {
            _paystackService = paystackService;
        }

        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment()
        {
            try
            {
                var userResponse = await _paystackService.GetCurrentUser();
                if (userResponse == null || !userResponse.IsSuccessful)
                    return Unauthorized(new { message = "User not authenticated." });

                var authorizationUrl = await _paystackService
                    .InitializePayment(userResponse.Value);

                return Ok(new { success = true, authorizationUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("verify/{reference}")]
        public async Task<IActionResult> VerifyPayment(string reference)
        {
            try
            {
                var userResponse = await _paystackService.GetCurrentUser();
                if (userResponse == null || !userResponse.IsSuccessful)
                    return Unauthorized(new { message = "User not authenticated." });

                bool isVerified = await _paystackService.VerifySubscriptionAsync(reference);

                return isVerified
                    ? Ok(new { success = true, message = "Payment verified successfully." })
                    : BadRequest(new { success = false, message = "Payment verification failed." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelSubscription([FromBody] string subscriptionCode)
        {
            try
            {
                var userResponse = await _paystackService.GetCurrentUser();
                if (userResponse == null || !userResponse.IsSuccessful)
                    return Unauthorized(new { message = "User not authenticated." });

                bool isCancelled = await _paystackService.CancelSubscriptionAsync(subscriptionCode);

                return isCancelled
                    ? Ok(new { success = true, message = "Subscription cancelled successfully." })
                    : BadRequest(new { success = false, message = "Subscription cancellation failed." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
