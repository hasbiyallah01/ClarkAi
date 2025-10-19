using ClarkAI.Core.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClarkAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaystackService _paystackService;

        public PaymentController(IPaystackService paystackService)
        {
            _paystackService = paystackService;
        }

        [HttpPost]
        public async Task<IActionResult> InitializePayment()
        {
            try
            {
                var userResponse = await _paystackService.GetCurrentUser();
                if (userResponse == null || !userResponse.IsSuccessful)
                    return Unauthorized(new { message = "User not authenticated" });

                var authorizationUrl = await _paystackService.InitializePayment(userResponse.Value.Id);

                return Ok(new { success = true, authorizationUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
