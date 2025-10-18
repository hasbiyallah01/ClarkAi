
using ClarkAI.Core.Application.Interfaces.Services;
using ClarkAI.Infrastructure;

namespace ClarkAI.Core.Application.Service
{
    public class PaymentProcessingJob : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentProcessingJob> _logger;

        public PaymentProcessingJob(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<PaymentProcessingJob> logger)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Deduction Sync Job Started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ClarkContext>();
                    var PaystackService = scope.ServiceProvider.GetRequiredService<IPaystackService>();

                    for (int i = 0; i < 10; i++)
                    {
                        Console.WriteLine("Im hasbiy");
                    }
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "");
                }
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
