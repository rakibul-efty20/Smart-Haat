using SmartHaat.Application.Commerce;

namespace SmartHaat.Api.Infrastructure;

public sealed class ReservationExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var commerce = scope.ServiceProvider.GetRequiredService<ICommerceService>();
                var expired = await commerce.ExpireReservationsAsync(stoppingToken);
                if (expired > 0) logger.LogInformation("Released expired inventory reservations for {OrderCount} order(s).", expired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "Could not release expired inventory reservations."); }
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
