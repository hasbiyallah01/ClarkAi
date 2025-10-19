using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Application.Interfaces.Services;
using ClarkAI.Core.Entity.Model;
using Microsoft.Extensions.Options;
using Paystack.Net.SDK;
using Paystack.Net.SDK.Models;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace ClarkAI.Core.Application.Service
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly PaystackSettings _paystackSetting;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUserRepository _userRepository;
        private readonly PayStackApi _paystack; 
        private readonly IHttpContextAccessor _contextAccessor;

        public PaystackService(HttpClient httpClient, IOptions<PaystackSettings> paystackSettings, IPaymentRepository paymentRepository, 
            IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _paystackSetting = paystackSettings.Value;
            _paymentRepository = paymentRepository;
            _userRepository = userRepository;
            _contextAccessor = httpContextAccessor;

            _paystack = new PayStackApi(paystackSettings.Value.SecretKey);
            _httpClient.BaseAddress = new Uri(_paystackSetting.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _paystackSetting.SecretKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<PaymentResponse> InitializePayment(int userId)
        {
            var user = await _userRepository.GetUser(userId);
            if (user == null)
                throw new Exception("User not found.");

            if (user.SubscriptionStatus == Entity.Enum.SubscriptionStatus.Active)
            {
                throw new Exception("User already has an active subscription.");
            }
            var trxRequest = new TransactionInitializationRequestModel
            {
                email = user.Email,
                amount = 10000
            };

            var trxResponse = await _paystack.Transactions.InitializeTransaction(trxRequest);

            if (!trxResponse.status) 
                throw new Exception($"Failed to initialize payment: {trxResponse.message}");

            var payment = new Payment
            {
                UserId = userId,
                Email = user.Email,
                Reference = trxResponse.data.reference,
                Amount = trxRequest.amount / 100M,
                Currency = "NGN",
                Status = Entity.Enum.PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow,
            };

            await _paymentRepository.AddAsync(payment);

            return new PaymentResponse
            {
                AuthorizationUrl = trxResponse.data.authorization_url,
                Reference = trxResponse.data.reference
            };
        }

        public async Task<BaseResponse<UserResponse>> GetCurrentUser()
        {
            var authHeader = _contextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
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

            var payload = System.Text.Json.JsonSerializer.Deserialize<JwtUser>(payloadJson);


            if (payload == null)
                return new BaseResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = "Invalid token Payload"
                };

            Console.WriteLine(payload.Id);
            var user = await _userRepository.GetUser(payload.Id);
            if (user == null)
            {
                return new BaseResponse<UserResponse>
                {
                    IsSuccessful = false,
                    Message = "User not found"
                };
            }

            return new BaseResponse<UserResponse>
            {
                IsSuccessful = true,
                Message = "User Authenticated",
                Value = new UserResponse
                {
                    Id = payload.Id,
                    Email = user.Email,
                    Name = user.Name,
                    PlanType = user.Plan,
                }
            };
        }
        public async Task<string> CreateRecurringSubscriptionAsync(int userId, string authorizationCode)
        {
            var user = await _userRepository.GetUser(userId);
            if (user == null)
                throw new Exception("User not found.");
            var startDate = user.NextBillingDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var subResponse = _paystack.Subscriptions.CreateSubscription(
                user.PaystackCustomerCode,
                "PLN_8xifaqj55u8ialb",
                authorizationCode,
                startDate
            );

            var result = subResponse.Result;

            if (!result.status && !result.message.Contains("Subscription successfully created", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Failed to create subscription: {result.message}");
            }

            var subscriptionCode = subResponse.Result.data.subscription_code;

            user.Plan = Entity.Enum.PlanType.Paid;
            user.SubscriptionStatus = Entity.Enum.SubscriptionStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            var payment = new Payment
            {
                UserId = userId,
                Email = user.Email,
                Reference = subscriptionCode,
                SubscriptionCode = subscriptionCode,
                Amount = 100000, 
                Currency = "NGN",
                Status = Entity.Enum.PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            return subscriptionCode;
        }

        public async Task<bool> CancelSubscriptionAsync(string subscriptionCode)
        {
            var payload = new { code = subscriptionCode };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("subscription/disable", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to disable subscription: {response.ReasonPhrase} - {responseString}");

            dynamic result = JsonConvert.DeserializeObject(responseString);
            if (result.status == true)
            {
                var payment = await _paymentRepository.GetBySubscriptionCodeAsync(subscriptionCode);
                if (payment != null)
                {
                    payment.Status = Entity.Enum.PaymentStatus.Failed;
                    payment.DateModified = DateTime.UtcNow;
                    await _paymentRepository.UpdateAsync(payment);
                }

                var user = await _userRepository.GetUser(payment.UserId);
                if (user != null)
                {
                    user.Plan = Entity.Enum.PlanType.Free;
                    user.SubscriptionStatus = Entity.Enum.SubscriptionStatus.Cancelled;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }

                return true;
            }

            return false;
        }

        public async Task<bool> VerifySubscriptionAsync(string reference)
        {
            var response = await _httpClient.GetAsync($"transaction/verify/{reference}");
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to verify payment: {response.ReasonPhrase} - {responseString}");

            dynamic result = JsonConvert.DeserializeObject(responseString);
            if (result.status != true)
                throw new Exception($"Verification failed: {result.message}");

            var data = result.data;

            if (data.status != "success")
                return false;

            var payment = await _paymentRepository.GetByReferenceAsync(reference);
            if (payment == null)
                throw new Exception("Payment record not found in the system.");

            if (payment.Status != Entity.Enum.PaymentStatus.Success)
            {
                payment.Status =    Entity.Enum.PaymentStatus.Success;
                payment.DateModified = DateTime.UtcNow;
                payment.SubscriptionCode = data.subscription?.subscription_code ?? payment.SubscriptionCode;
                await _paymentRepository.UpdateAsync(payment);
            }

            var user = await _userRepository.GetUser(payment.UserId);
            if (user != null)
            {
                user.Plan = Entity.Enum.PlanType.Paid;
                user.SubscriptionStatus = Entity.Enum.SubscriptionStatus.Active;
                user.UpdatedAt = DateTime.UtcNow;

                user.PaystackAuthorizationCode = data.authorization?.authorization_code ?? user.PaystackAuthorizationCode;
                user.PaystackCustomerCode = data.customer?.customer_code ?? user.PaystackCustomerCode;

                var now = DateTime.UtcNow;
                user.NextBillingDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc).AddMonths(1);

                await _userRepository.UpdateAsync(user);

                if (string.IsNullOrEmpty(payment.SubscriptionCode))
                {
                    var sub = await CreateRecurringSubscriptionAsync(user.Id, user.PaystackAuthorizationCode);
                    if (!string.IsNullOrEmpty(sub))
                    {
                        user.SubscriptionStatus = Entity.Enum.SubscriptionStatus.Active;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _userRepository.UpdateAsync(user);
                    }
                }
            }

            return true;
        }
        private static string Base64UrlDecode(string input)
        {
            string output = input.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }

            var bytes = Convert.FromBase64String(output);
            return Encoding.UTF8.GetString(bytes);
        }

        
    }
    public class JwtUser
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
}
