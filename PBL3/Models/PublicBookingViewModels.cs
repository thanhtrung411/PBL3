using System.ComponentModel.DataAnnotations;

namespace PBL3.Models;

public class RoomSearchViewModel
{
    public DateTime CheckIn { get; set; } = DateTime.Today;
    public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);
    public int Guests { get; set; } = 2;
    public string? RoomTypeId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<PublicRoomOptionViewModel> Results { get; set; } = new();
}

public class PublicRoomOptionViewModel
{
    public string RoomTypeId { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Description { get; set; } = "";
    public int MaxGuests { get; set; }
    public decimal PricePerNight { get; set; }
    public int AvailableRooms { get; set; }
}

public class BookingLookupViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã đặt phòng.")]
    [StringLength(10)]
    public string BookingCode { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Số điện thoại chỉ gồm 10-15 chữ số.")]
    [StringLength(15)]
    public string PhoneNumber { get; set; } = "";

    public bool HasSearched { get; set; }
    public bool Found => Result != null;
    public BookingLookupResultViewModel? Result { get; set; }
}

public class BookingLookupResultViewModel
{
    public string BookingCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public string Status { get; set; } = "";
    public string RoomSummary { get; set; } = "";
    public decimal TotalAmount { get; set; }
}

public class PublicBookingResult
{
    public bool Success { get; set; }
    public string? BookingCode { get; set; }
    public string? ErrorMessage { get; set; }
}
