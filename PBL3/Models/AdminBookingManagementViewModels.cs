namespace PBL3.Models;

public class AdminBookingManagementViewModel
{
    public List<AdminBookingManagementItemViewModel> Bookings { get; set; } = new();

    public int ArrivalCount => Bookings.Count(x => x.FilterStatuses.Contains("arrival"));

    public int StayingCount => Bookings.Count(x => x.FilterStatuses.Contains("staying"));

    public int DepartureCount => Bookings.Count(x => x.FilterStatuses.Contains("departure"));

    public int TotalCount => Bookings.Count;
}

public class AdminBookingManagementItemViewModel
{
    public string BookingCode { get; set; } = string.Empty;

    public string GuestName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? IdentityNumber { get; set; }

    public string RoomSummary { get; set; } = string.Empty;

    public string RoomNumbers { get; set; } = string.Empty;

    public string RoomTypes { get; set; } = string.Empty;

    public string CheckInLabel { get; set; } = string.Empty;

    public string CheckOutLabel { get; set; } = string.Empty;

    public string DateRangeLabel { get; set; } = string.Empty;

    public string NightsLabel { get; set; } = string.Empty;

    public string GuestsLabel { get; set; } = string.Empty;

    public string TotalLabel { get; set; } = string.Empty;

    public string PaidLabel { get; set; } = string.Empty;

    public string PaymentStatusLabel { get; set; } = string.Empty;

    public string PaymentStatusClass { get; set; } = "unpaid";

    public string BookingStatusLabel { get; set; } = string.Empty;

    public string BookingStatusClass { get; set; } = "upcoming";

    public string BookingStatusIcon { get; set; } = "bi-calendar-check";

    public string FilterStatus { get; set; } = "all";

    public string FilterStatuses { get; set; } = "all";

    public string CreatedAtLabel { get; set; } = string.Empty;
}
