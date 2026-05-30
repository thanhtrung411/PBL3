namespace PBL3.Models;

using Microsoft.AspNetCore.Http;

public class AdminRoomTypeIndexViewModel
{
    public List<AdminRoomTypeCardViewModel> RoomTypes { get; set; } = new();
}

public class AdminRoomTypeCardViewModel
{
    public string RoomTypeId { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;

    public int MaxGuests { get; set; }

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = "/images/booking_hero.jpg";

    public decimal? CurrentPrice { get; set; }

    public string CurrentPriceSource { get; set; } = "Chưa có bảng giá phù hợp";

    public AdminRoomTypePriceOverviewViewModel PriceOverview { get; set; } = new();

    public int TotalRooms { get; set; }

    public int AvailableRooms { get; set; }

    public int OccupiedRooms { get; set; }

    public int MaintenanceRooms { get; set; }

    public List<AdminRoomSummaryViewModel> Rooms { get; set; } = new();
}

public class AdminRoomSummaryViewModel
{
    public string RoomId { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public int Floor { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class AdminRoomTypePriceOverviewViewModel
{
    public decimal? DefaultPrice { get; set; }

    public List<AdminRoomTypeWeekdayPriceViewModel> WeekdayPrices { get; set; } = new();

    public List<AdminRoomTypeHolidayPriceViewModel> HolidayPrices { get; set; } = new();
}

public class AdminRoomTypeWeekdayPriceViewModel
{
    public byte DayValue { get; set; }

    public string DayLabel { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public decimal Multiplier { get; set; } = 1;

    public bool IsChanged { get; set; }
}

public class AdminRoomTypeHolidayPriceViewModel
{
    public string PriceId { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal Price { get; set; }

    public decimal Multiplier { get; set; } = 1;

    public string Note { get; set; } = string.Empty;
}

public class AdminRoomTypePriceUpdateRequest
{
    public string RoomTypeId { get; set; } = string.Empty;

    public decimal DefaultPrice { get; set; }

    public List<AdminRoomTypeWeekdayMultiplierRequest> WeekdayMultipliers { get; set; } = new();
}

public class AdminRoomTypeWeekdayMultiplierRequest
{
    public byte DayValue { get; set; }

    public decimal Multiplier { get; set; }
}

public class AdminRoomTypeHolidayPriceCreateRequest
{
    public string RoomTypeId { get; set; } = string.Empty;

    public DateOnly HolidayDate { get; set; }

    public int DaysBefore { get; set; }

    public int DaysAfter { get; set; }

    public decimal Multiplier { get; set; }
}

public class AdminRoomTypeCreateViewModel
{
    public string RoomTypeId { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;

    public int MaxGuests { get; set; } = 2;

    public string? Description { get; set; }

    public decimal DefaultPrice { get; set; }

    public List<AdminRoomTypeWeekdayMultiplierRequest> WeekdayMultipliers { get; set; } = new();

    public List<IFormFile> Images { get; set; } = new();
}
