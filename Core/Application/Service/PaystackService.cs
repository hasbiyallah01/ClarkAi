using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Application.Interfaces.Services;
using ClarkAI.Core.Entity.Model;
using Microsoft.Extensions.Options;
using Paystack.Net.SDK;
using Paystack.Net.SDK.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

namespace ClarkAI.Core.Application.Service
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly PaystackSettings _paystackSetting;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUserRepository _userRepository;
        private readonly PayStackApi _payStackApi; 
        private readonly IHttpContextAccessor _contextAccessor;

        public PaystackService(HttpClient httpClient, IOptions<PaystackSettings> paystackSettings, IPaymentRepository paymentRepository, 
            IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _paystackSetting = paystackSettings.Value;
            _paymentRepository = paymentRepository;
            _userRepository = userRepository;
            _contextAccessor = httpContextAccessor;

            _payStackApi = new PayStackApi(paystackSettings.Value.SecretKey);
            _httpClient.BaseAddress = new Uri(_paystackSetting.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _paystackSetting.SecretKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public Task<bool> CancelSubscriptionAsync(string subscriptionCode)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateRecurringSubscriptionAsync(int userId, string authCode)
        {
            throw new NotImplementedException();
        }

        public async Task<PaymentResponse> InitializePayment(int userId)
        {
            var user = await _userRepository.GetUser(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            if(user.SubscriptionStatus == Entity.Enum.SubscriptionStatus.Active)
            {
                throw new Exception("User already has active Subscription");
            }

            var trxRequest = new TransactionInitializationRequestModel
            {
                email = user.Email,
                amount = 10000
            };

            var trxResponse = await _payStackApi.Transactions.InitializeTransaction(trxRequest);

            if(!trxResponse.status)
            {
                throw new Exception($"Failed to initialize Paymet: {trxResponse.message}");
            }

            var payment = new Payment
            {
                UserId = userId,
                Email = user.Email,
                Amount = trxRequest.amount/100M,
                Currency = "NGN",
                Reference = trxResponse.data.reference,
                Status = Entity.Enum.PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
            };
            await _paymentRepository.AddAsync(payment);

            return new PaymentResponse
            {
                AuthorizationUrl = trxResponse.data.authorization_url,
                Reference = trxResponse.data.reference,
            };
        }

        public Task<bool> VerifySubscription(string reference)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<UserResponse>> GetCurrentUser()
        {
            var authHeader = _contextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if(string.IsNullOrEmpty(authHeader))
                return new BaseResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = "Unauthorized"
                };

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var parts = token.Split('.');
            if (parts.Length != 3)
                return new BaseResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = "Invalid Token"
                };

            var payloadJson = Base64UrlDecode(parts[1]);

            var payload = JsonSerializer.Deserialize<JwtUser>(payloadJson);

            if (payload == null)
                return new BaseResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = "Invalid token Payload"
                };


        }

        public class JwtUser
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }
        }
    }
}
