using PBL3.Models;

namespace PBL3.Services.Interfaces;

public interface IPublicBookingService
{
    Task<RoomSearchViewModel> SearchRoomsAsync(DateTime? checkIn, DateTime? checkOut, int? guests, string? roomTypeId);
    Task<CheckoutViewModel?> BuildCheckoutAsync(string roomTypeId, DateTime? checkIn, DateTime? checkOut, int? guests);
    Task<PublicBookingResult> ConfirmBookingAsync(CheckoutViewModel model);
    Task<BookingLookupResultViewModel?> LookupAsync(string bookingCode, string phoneNumber);
}
