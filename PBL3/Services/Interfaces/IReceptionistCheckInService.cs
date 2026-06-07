using PBL3.Services.Receptionist;

namespace PBL3.Services.Interfaces;

public interface IReceptionistCheckInService
{
    Task<ReceptionistBookingLookupResult> LookupBookingAsync(string input, CancellationToken cancellationToken = default);

    Task<ReceptionistTodayArrivalsResult> GetTodayArrivalsAsync(CancellationToken cancellationToken = default);

    Task<ReceptionistRoomMapResult> GetRoomMapAsync(CancellationToken cancellationToken = default);

    Task<RoomMaintenanceResult> SetRoomMaintenanceAsync(RoomMaintenanceRequest request, CancellationToken cancellationToken = default);

    Task<ReceptionistServiceUsageResult> GetServiceUsageAsync(CancellationToken cancellationToken = default);

    Task<ReceptionistAddServiceResult> AddServiceUsageAsync(ReceptionistAddServiceRequest request, CancellationToken cancellationToken = default);

    Task<ReceptionistCheckoutListResult> GetCheckoutListAsync(CancellationToken cancellationToken = default);

    Task<ReceptionistCheckoutResult> CheckoutAsync(ReceptionistCheckoutRequest request, CancellationToken cancellationToken = default);

    Task<ReceptionistRoomSelectionResult> GetRoomSelectionAsync(string bookingCode, CancellationToken cancellationToken = default);

    Task<ReceptionistCheckInResult> CheckInAsync(ReceptionistCheckInRequest request, CancellationToken cancellationToken = default);

    Task<ReceptionistWalkInAvailabilityResult> GetWalkInAvailabilityAsync(ReceptionistWalkInAvailabilityRequest request, CancellationToken cancellationToken = default);

    Task<ReceptionistWalkInPromotionPreviewResult> PreviewWalkInPromotionAsync(ReceptionistWalkInPromotionPreviewRequest request, CancellationToken cancellationToken = default);

    Task<ReceptionistWalkInCheckInResult> WalkInCheckInAsync(ReceptionistWalkInCheckInRequest request, CancellationToken cancellationToken = default);

    Task CancelWalkInPendingPaymentAsync(string bookingCode, CancellationToken cancellationToken = default);

    Task ProcessOverdueBookingsAsync(DateTime currentTime, CancellationToken cancellationToken = default);
}
