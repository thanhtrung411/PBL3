namespace PBL3.Services.Interfaces;

public interface IExpiredBookingCleanupService
{
    Task<int> CancelExpiredOnlinePaymentsAsync(CancellationToken cancellationToken = default);

    Task<int> MarkExpiredNoShowBookingsAsync(CancellationToken cancellationToken = default);
}
