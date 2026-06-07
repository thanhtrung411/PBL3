using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PBL3.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PBL3.Services;

public class OverdueCheckoutWorker : BackgroundService
{
    private readonly ILogger<OverdueCheckoutWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public OverdueCheckoutWorker(ILogger<OverdueCheckoutWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OverdueCheckoutWorker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOverdueBookingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing OverdueCheckoutWorker.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("OverdueCheckoutWorker is stopping.");
    }

    private async Task ProcessOverdueBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var receptionistCheckInService = scope.ServiceProvider.GetRequiredService<IReceptionistCheckInService>();
        var currentTime = DateTime.Now;

        _logger.LogInformation("Checking for overdue checkouts at: {time}", currentTime);
        
        await receptionistCheckInService.ProcessOverdueBookingsAsync(currentTime, cancellationToken);
    }
}
