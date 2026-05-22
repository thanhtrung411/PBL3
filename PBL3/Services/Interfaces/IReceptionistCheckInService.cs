using PBL3.Services.Receptionist;

namespace PBL3.Services.Interfaces;

public interface IReceptionistCheckInService
{
    Task<ReceptionistBookingLookupResult> LookupBookingAsync(string input, CancellationToken cancellationToken = default);

    Task<ReceptionistRoomSelectionResult> GetRoomSelectionAsync(string bookingCode, CancellationToken cancellationToken = default);

    Task<ReceptionistCheckInResult> CheckInAsync(ReceptionistCheckInRequest request, CancellationToken cancellationToken = default);
}
