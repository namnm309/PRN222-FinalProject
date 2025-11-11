using BusinessLayer.Services;
using Microsoft.Extensions.Hosting;

namespace PresentationLayer.Services
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6);

        public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RefreshTokenCleanupService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                    await authService.CleanExpiredTokensAsync();
                    _logger.LogInformation("Expired refresh tokens cleaned at {Time}", DateTimeOffset.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while cleaning refresh tokens");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("RefreshTokenCleanupService stopped");
        }
    }
}

