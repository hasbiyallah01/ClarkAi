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
using ClarkAI.Core.Entity.Enum;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClarkAI.Core.Application.Service
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _httpClient;
        private readonly PaystackSettings _paystackSetting;
        private readonly ILogger<PaystackService> _logger;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;
        private readonly PayStackApi _paystack; 
        private readonly IHttpContextAccessor _contextAccessor;

        public PaystackService(HttpClient httpClient, IOptions<PaystackSettings> paystackSettings, IPaymentRepository paymentRepository, 
            IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork, ILogger<PaystackService> logger)
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
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PaymentResponse> InitializePayment(int userId)
        {
            var user = await _userRepository.GetUser(userId);
            if (user == null)
                throw new Exception("User not found.");

            if (user.SubscriptionStatus == Entity.Enum.SubscriptionStatus.Active)
                throw new Exception("User already has an active subscription.");

            var trxRequest = new TransactionInitializationRequestModel
            {
                email = user.Email,
                amount = 10000
            };

            var trxResponse = await _paystack.Transactions.InitializeTransaction(trxRequest);

            if (!trxResponse.status)
                throw new Exception($"Failed to initialize payment: {trxResponse.message}");

            var existingPayment = await _paymentRepository.GetByReferenceAsync(trxResponse.data.reference);

            if (existingPayment == null)
            {
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


                try
                {
                    await _paymentRepository.AddAsync(payment);
                    await _unitOfWork.SaveAsync();
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException is PostgresException pgEx &&
                        pgEx.SqlState == "23505" && pgEx.ConstraintName == "payments_reference_key")
                    {
                        _logger.LogWarning("Duplicate payment reference detected: {Reference}", trxResponse.data.reference);
                    }
                    else
                    {
                        throw;
                    }
                }

            }

            return new PaymentResponse
            {
                AuthorizationUrl = trxResponse.data.authorization_url,
                Reference = trxResponse.data.reference
            };
        }


        public async Task<BaseResponse<int>> GetCurrentUser()
        {
            var authHeader = _contextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
                return new BaseResponse<int>
                {
                    IsSuccessful = false,
                    Message = "Unauthorized"
                };

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var parts = token.Split('.');
            if (parts.Length != 3)
                return new BaseResponse<int>
                {
                    IsSuccessful = false,
                    Message = "Invalid Token"
                };

            var payloadJson = Base64UrlDecode(parts[1]);

            var payload = System.Text.Json.JsonSerializer.Deserialize<JwtUser>(payloadJson);


            if (payload == null)
                return new BaseResponse<int>
                {
                    IsSuccessful = false,
                    Message = "Invalid token Payload"
                };

            Console.WriteLine(payload.Id);
            var user = await _userRepository.Exist(payload.Id);
            if (!user)
            {
                return new BaseResponse<int>
                {
                    IsSuccessful = false,
                    Message = "User not found"
                };
            }

            return new BaseResponse<int>
            {
                IsSuccessful = true,
                Message = "User succesfully retrieved",
                Value = payload.Id
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
                "PLN_uq27mhh0g6y2qw8",
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

            try
            {
                await _paymentRepository.AddAsync(payment);
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx &&
                    pgEx.SqlState == "23505" && pgEx.ConstraintName == "payments_reference_key")
                {
                    _logger.LogWarning("Duplicate payment reference detected");
                }
                else
                {
                    throw;
                }
            }
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

            if (result.status != true)
                throw new Exception($"Paystack returned error: {result.message}");

            var payment = await _paymentRepository.GetBySubscriptionCodeAsync(subscriptionCode);
            if (payment == null)
                throw new Exception("Payment not found for this subscription code");

            payment.Status = Entity.Enum.PaymentStatus.Cancelled;
            payment.DateModified = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);
            await _unitOfWork.SaveAsync();

            var user = await _userRepository.GetUser(payment.UserId);
            if (user != null)
            {
                user.Plan = Entity.Enum.PlanType.Free;
                user.SubscriptionStatus = Entity.Enum.SubscriptionStatus.Cancelled;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveAsync();
            }

            return true;
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

            var payment = await _paymentRepository.GetByReferenceAsync(reference)
                ?? throw new Exception("Payment record not found in the system.");

            if (payment.Status != PaymentStatus.Success)
            {
                payment.Status = PaymentStatus.Success;
                payment.DateModified = DateTime.UtcNow;
                payment.SubscriptionCode = data.subscription?.subscription_code ?? payment.SubscriptionCode;
                await _paymentRepository.UpdateAsync(payment);
                await _unitOfWork.SaveAsync();
            }

            var user = await _userRepository.GetUser(payment.UserId);
            if (user == null) return true;

            user.Plan = PlanType.Paid;
            user.SubscriptionStatus = SubscriptionStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;
            user.PaystackAuthorizationCode = data.authorization?.authorization_code ?? user.PaystackAuthorizationCode;
            user.PaystackCustomerCode = data.customer?.customer_code ?? user.PaystackCustomerCode;
            user.NextBillingDate = DateTime.UtcNow.AddMonths(1);

            if (string.IsNullOrEmpty(payment.SubscriptionCode))
            {
                var sub = await CreateRecurringSubscriptionAsync(user.Id, user.PaystackAuthorizationCode);
                if (!string.IsNullOrEmpty(sub))
                {
                    user.SubscriptionStatus = SubscriptionStatus.Active;
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _userRepository.UpdateAsync(user);
            try
            {

                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx &&
                    pgEx.SqlState == "23505" && pgEx.ConstraintName == "payments_reference_key")
                {
                    _logger.LogWarning("Duplicate payment reference detected");
                }
                else
                {
                    throw;
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
