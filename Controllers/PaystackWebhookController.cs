using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Application.Interfaces.Services;
using ClarkAI.Core.Entity.Enum;
using ClarkAI.Core.Entity.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace ClarkAI.Controllers
{
    [Route("api/webhooks/paystack")]
    [ApiController]
    public class PaystackWebhookController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaystackService _paystackService;
        private readonly PaystackSettings _paystackSettings;
        private readonly ILogger<PaystackWebhookController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public PaystackWebhookController(IPaymentRepository paymentRepository, IUserRepository userRepository, PaystackSettings
            paystackSettings, ILogger<PaystackWebhookController> logger, IUnitOfWork unitOfWork, IPaystackService paystackService)
        {
            _paymentRepository = paymentRepository;
            _userRepository = userRepository;
            _paystackSettings = paystackSettings;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _paystackService = paystackService;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var hash = ComputeHash(_paystackSettings.WebhookSecret, body);
            var headerSignature = Request.Headers["x-paystack-signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(headerSignature) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hash),
                    Encoding.UTF8.GetBytes(headerSignature)))
            {
                _logger.LogWarning("Paystack webhook signature validation failed.");
                return Unauthorized();
            }

            JObject payload;
            try
            {
                payload = JObject.Parse(body);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Paystack webhook payload.");
                return BadRequest();
            }

            var eventType = payload["event"]?.ToString();
            var data = payload["data"];

            try
            {
                switch (eventType)
                {
                    case "charge.success":
                        await HandleChargeSuccess(data);
                        break;
                    case "charge.failed":
                        await HandleChargeFailed(data);
                        break;
                    case "subscription.create":
                        await HandleSubscriptionCreate(data);
                        break;
                    case "subscription.disable":
                        await HandleSubscriptionDisable(data);
                        break;
                    default:
                        _logger.LogInformation($"Unhandled Paystack webhook event: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling Paystack webhook event: {eventType}");
            }

            return Ok();
        }


        private static string ComputeHash(string secret, string body)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(bodyBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private async Task HandleSubscriptionCreate(JToken data)
        {
            string subscriptionCode = data["subscription_code"]?.ToString();
            string customerCode = data["customer"]?["customer_code"]?.ToString();
            string authorizationCode = data["authorization"]?["authorization_code"]?.ToString();
            string email = data["customer"]?["email"]?.ToString();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(subscriptionCode))
            {
                _logger.LogWarning("Subscription creation webhook missing required fields.");
                return;
            }

            var user = await _userRepository.GetAsync(a => a.Email == email);
            if (user != null)
            {
                user.PaystackCustomerCode = customerCode ?? user.PaystackCustomerCode;
                user.PaystackAuthorizationCode = authorizationCode ?? user.PaystackAuthorizationCode;
                user.SubscriptionStatus = SubscriptionStatus.Active;
                user.Plan = PlanType.Paid;
                user.UpdatedAt = DateTime.UtcNow;

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
            }

            var existingPayment = await _paymentRepository.GetBySubscriptionCodeAsync(subscriptionCode);

            if (existingPayment != null)
            {
                existingPayment.Status = Core.Entity.Enum.PaymentStatus.Success;
                existingPayment.DateModified = DateTime.UtcNow;
                existingPayment.ConfirmedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(existingPayment);
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
            }
            else if (user != null) 
            {
                var amountInKobo = data["amount"]?.ToObject<decimal?>() ?? 0M;
                var payment = new Payment
                {
                    UserId = user.Id,
                    Email = email,
                    Reference = subscriptionCode,
                    SubscriptionCode = subscriptionCode,
                    Amount = amountInKobo / 100M,
                    Currency = "NGN",
                    Status = PaymentStatus.Success,
                    PaymentDate = DateTime.UtcNow,
                    ConfirmedAt = DateTime.UtcNow,
                    DateCreated = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow
                };

                try
                {
                    await _paymentRepository.AddAsync(payment);
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
            }
        }


        private async Task HandleSubscriptionDisable(JToken data)
        {
            string subscriptionCode = data["subscription_code"]?.ToString();

            var payment = await _paymentRepository.GetBySubscriptionCodeAsync(subscriptionCode);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Failed;
                payment.DateModified = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                var user = await _userRepository.GetUser(payment.UserId);
                if (user != null)
                {
                    user.Plan = PlanType.Free;
                    user.SubscriptionStatus = SubscriptionStatus.Cancelled;
                    user.UpdatedAt = DateTime.UtcNow;
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
                }
            }
        }

        private async Task HandleChargeSuccess(JToken data)
        {
            string reference = data["reference"]?.ToString();
            var payment = await _paymentRepository.GetByReferenceAsync(reference);
            if (payment == null)
                return;

            payment.Status = PaymentStatus.Success;
            payment.ConfirmedAt = DateTime.UtcNow;
            payment.DateModified = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);

            var user = await _userRepository.GetUser(payment.UserId);
            if (user == null)
                return;

            user.Plan = PlanType.Paid;
            user.SubscriptionStatus = SubscriptionStatus.Active;
            user.PaystackCustomerCode = data["customer"]?["customer_code"]?.ToString() ?? user.PaystackCustomerCode;
            user.PaystackAuthorizationCode = data["authorization"]?["authorization_code"]?.ToString() ?? user.PaystackAuthorizationCode;

            if (DateTime.TryParse(data["next_payment_date"]?.ToString(), out var nextPaymentDate) &&
                nextPaymentDate != default)
            {
                user.NextBillingDate = nextPaymentDate;
            }

            user.UpdatedAt = DateTime.UtcNow;
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

            if (string.IsNullOrEmpty(payment.SubscriptionCode))
            {
                if (!string.IsNullOrEmpty(user.PaystackAuthorizationCode) && !string.IsNullOrEmpty(user.PaystackCustomerCode))
                {
                    var subCode = await _paystackService.CreateRecurringSubscriptionAsync(user.Id, user.PaystackAuthorizationCode);
                    if (!string.IsNullOrEmpty(subCode))
                    {
                        payment.SubscriptionCode = subCode;
                        await _paymentRepository.UpdateAsync(payment);
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
                    }
                }
                else
                {
                    _logger.LogWarning("Missing authorization or customer code; cannot activate recurring subscription yet.");
                }
            }
        }




        private async Task HandleChargeFailed(JToken data)
        {
            string reference = data["reference"]?.ToString();

            var payment = await _paymentRepository.GetByReferenceAsync(reference);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Failed;
                payment.DateModified = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
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

                var user = await _userRepository.GetUser(payment.UserId);
                if (user != null)
                {
                    user.SubscriptionStatus = SubscriptionStatus.Failed;
                    user.Plan = PlanType.Free;
                    user.UpdatedAt = DateTime.UtcNow;
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
                }

            }
            return;

        }
    }
}
