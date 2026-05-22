namespace PBL3.Services.Receptionist;

public class ReceptionistCheckInRequest
{
    public string? BookingCode { get; set; }

    public List<string> RoomIds { get; set; } = new();
}

public class ReceptionistBookingLookupResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public ReceptionistBookingDto? Booking { get; set; }
}

public class ReceptionistRoomSelectionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public List<ReceptionistRoomTypeRequirementDto> Requirements { get; set; } = new();

    public List<ReceptionistRoomGroupDto> Groups { get; set; } = new();
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
}

public class ReceptionistAssignedRoomDto
{
    public string RoomId { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string RoomTypeId { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;
}
