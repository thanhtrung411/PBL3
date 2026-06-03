namespace PBL3.Services.Receptionist;

public class ReceptionistCheckInRequest
{
    public string? BookingCode { get; set; }

    public List<string> RoomIds { get; set; } = new();
}

public class ReceptionistWalkInAvailabilityRequest
{
    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }
}

public class ReceptionistWalkInAvailabilityResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string CheckInDate { get; set; } = string.Empty;

    public string CheckOutDate { get; set; } = string.Empty;

    public int Nights { get; set; }

    public List<ReceptionistRoomGroupDto> Groups { get; set; } = new();
}

public class ReceptionistWalkInPromotionPreviewRequest
{
    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public List<string> RoomIds { get; set; } = new();
}

public class ReceptionistWalkInPromotionPreviewResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public decimal RoomTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public string? PromotionCode { get; set; }

    public string? PromotionName { get; set; }
}

public class ReceptionistWalkInCheckInRequest
{
    public string CustomerName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? IdentityNumber { get; set; }

    public string? Email { get; set; }

    public string? Gender { get; set; }

    public string? Nationality { get; set; }

    public string? Address { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int GuestCount { get; set; }

    public List<string> RoomIds { get; set; } = new();

    public decimal PaymentAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Note { get; set; }

    public string? EmployeeId { get; set; }
}

public class ReceptionistWalkInCheckInResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public string InvoiceCode { get; set; } = string.Empty;

    public decimal GrandTotal { get; set; }

    public bool RequiresOnlinePayment { get; set; }

    public string PaymentUrl { get; set; } = string.Empty;

    public List<ReceptionistAssignedRoomDto> AssignedRooms { get; set; } = new();
}

public class ReceptionistBookingLookupResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public ReceptionistBookingDto? Booking { get; set; }
}

public class ReceptionistTodayArrivalsResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string DateLabel { get; set; } = string.Empty;

    public List<ReceptionistBookingDto> Bookings { get; set; } = new();
}

public class ReceptionistRoomSelectionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public List<ReceptionistRoomTypeRequirementDto> Requirements { get; set; } = new();

    public List<ReceptionistRoomGroupDto> Groups { get; set; } = new();
}

public class ReceptionistRoomMapResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<ReceptionistRoomMapStatusCountDto> StatusCounts { get; set; } = new();

    public List<ReceptionistFloorRoomMapDto> Floors { get; set; } = new();
}

public class RoomMaintenanceRequest
{
    public string? RoomId { get; set; }

    public bool Maintenance { get; set; }
}

public class RoomMaintenanceResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string RoomId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;
}

public class ReceptionistServiceUsageResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<ReceptionistActiveStayDto> ActiveStays { get; set; } = new();

    public List<ReceptionistServiceOptionDto> Services { get; set; } = new();
}

public class ReceptionistCheckoutListResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<ReceptionistActiveStayDto> ActiveStays { get; set; } = new();
}

public class ReceptionistCheckoutRequest
{
    public string? BookingCode { get; set; }

    public decimal PaymentAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Note { get; set; }

    public bool SendReceiptEmail { get; set; } = true;

    public string? ReceiptEmail { get; set; }

    public bool PrintInvoice { get; set; } = true;
}

public class ReceptionistCheckoutResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public decimal PaidAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal RemainingAmount { get; set; }

    public bool RequiresOnlinePayment { get; set; }

    public string PaymentUrl { get; set; } = string.Empty;

    public bool EmailSent { get; set; }

    public string EmailMessage { get; set; } = string.Empty;

    public List<string> ReleasedRooms { get; set; } = new();
}

public class ReceptionistAddServiceRequest
{
    public string? BookingCode { get; set; }

    public string? ServiceId { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }
}

public class ReceptionistAddServiceResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public decimal ServiceTotal { get; set; }

    public decimal GrandTotal { get; set; }

    public ReceptionistServiceLineDto? AddedLine { get; set; }
}

public class ReceptionistActiveStayDto
{
    public string BookingCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string CheckInDate { get; set; } = string.Empty;

    public string CheckOutDate { get; set; } = string.Empty;

    public int Nights { get; set; }

    public string InvoiceStatus { get; set; } = string.Empty;

    public decimal RoomTotal { get; set; }

    public decimal ServiceTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public string? PromotionCode { get; set; }

    public string? PromotionName { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal RemainingAmount { get; set; }

    public List<string> RoomNumbers { get; set; } = new();

    public List<ReceptionistRoomChargeLineDto> RoomLines { get; set; } = new();

    public List<ReceptionistServiceLineDto> ServiceLines { get; set; } = new();
}

public class ReceptionistRoomChargeLineDto
{
    public string LineId { get; set; } = string.Empty;

    public string RoomTypeId { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;

    public string? RoomNumber { get; set; }

    public int Guests { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Total { get; set; }
}

public class ReceptionistServiceOptionDto
{
    public string ServiceId { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string? Category { get; set; }

    public decimal UnitPrice { get; set; }
}

public class ReceptionistServiceLineDto
{
    public string LineId { get; set; } = string.Empty;

    public string ServiceId { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Total { get; set; }

    public string AppliedDate { get; set; } = string.Empty;

    public string? Note { get; set; }
}

public class ReceptionistRoomMapStatusCountDto
{
    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class ReceptionistFloorRoomMapDto
{
    public int Floor { get; set; }

    public List<ReceptionistRoomDto> Rooms { get; set; } = new();
}

public class ReceptionistCheckInResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public List<ReceptionistAssignedRoomDto> AssignedRooms { get; set; } = new();
}

public class ReceptionistBookingDto
{
    public string BookingCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? IdentityNumber { get; set; }

    public string BookingDate { get; set; } = string.Empty;

    public string CheckInDate { get; set; } = string.Empty;

    public string CheckOutDate { get; set; } = string.Empty;

    public int Nights { get; set; }

    public string BookingStatus { get; set; } = string.Empty;

    public string InvoiceStatus { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public bool CanCheckIn { get; set; }

    public string CheckInMessage { get; set; } = string.Empty;

    public List<ReceptionistRoomTypeRequirementDto> Requirements { get; set; } = new();

    public List<ReceptionistAssignedRoomDto> AssignedRooms { get; set; } = new();
}

public class ReceptionistRoomTypeRequirementDto
{
    public string RoomTypeId { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;

    public int RequiredRooms { get; set; }

    public int Guests { get; set; }
}

public class ReceptionistRoomGroupDto
{
    public string RoomTypeId { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;

    public int RequiredRooms { get; set; }

    public int MaxSelectableRooms { get; set; }

    public int ReservedForBookingCount { get; set; }

    public decimal PricePerNight { get; set; }

    public int Capacity { get; set; }

    public List<ReceptionistRoomDto> Rooms { get; set; } = new();
}

public class ReceptionistRoomDto
{
    public string RoomId { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string RoomTypeId { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;

    public int Floor { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public bool IsSelectable { get; set; }

    public decimal PricePerNight { get; set; }

    public int Capacity { get; set; }

    public string? BookingCode { get; set; }

    public string? CurrentGuestName { get; set; }

    public string? CheckOutDate { get; set; }
}

public class ReceptionistAssignedRoomDto
{
    public string RoomId { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string RoomTypeId { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;
}
