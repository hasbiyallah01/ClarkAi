using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClarkAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> InitializePayment()
        {
            try
            {

            }
        }
    }
}
