using ETFPay.Services;

namespace ETFPay.Services
{
    // Servis koji periodicno provjerava i izvrsava subskripcije
    public class PretplatePaymentBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PretplatePaymentBackgroundService> _logger;

        public PretplatePaymentBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PretplatePaymentBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Pretplate Payment Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var pretplateService = scope.ServiceProvider.GetRequiredService<IPretplateService>();
                        await pretplateService.ExecuteDuePaymentsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while executing subscription payments");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }

            _logger.LogInformation("Pretplate Payment Background Service is stopping");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pretplate Payment Background Service is being stopped");
            return base.StopAsync(cancellationToken);
        }
    }
}
