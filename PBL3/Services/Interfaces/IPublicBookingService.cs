using PBL3.Models;

namespace PBL3.Services.Interfaces;

public interface IPublicBookingService
{
    Task<RoomSearchViewModel> SearchRoomsAsync(DateTime? checkIn, DateTime? checkOut, int? guests, string? roomTypeId, int? rooms = null);
    Task<CheckoutViewModel?> BuildCheckoutAsync(string? roomTypeId, DateTime? checkIn, DateTime? checkOut, int? guests, int? rooms = null, string? roomSelection = null, bool capacityWarningConfirmed = false);
    Task<PublicBookingResult> ConfirmBookingAsync(CheckoutViewModel model);
    Task<BookingLookupResultViewModel?> LookupAsync(string bookingCode, string phoneNumber);
}
