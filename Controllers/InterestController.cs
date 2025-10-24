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
    }
}
