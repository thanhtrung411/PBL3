using PBL3.Services.Interfaces;

namespace PBL3.Services;

public class ExpiredBookingCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredBookingCleanupHostedService> _logger;

    public ExpiredBookingCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredBookingCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<IExpiredBookingCleanupService>();
                await cleanup.CancelExpiredOnlinePaymentsAsync(stoppingToken);
                await cleanup.MarkExpiredNoShowBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Expired booking cleanup failed.");
            }
        }
    }
}
