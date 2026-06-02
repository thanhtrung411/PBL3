namespace PBL3.Models;

public class AdminCustomerManagementViewModel
{
    public List<AdminCustomerManagementItemViewModel> Customers { get; set; } = new();

    public int TotalCount => Customers.Count;

    public int RegularCount => Customers.Count(x => x.MemberTier == "Thường");

    public int SilverCount => Customers.Count(x => x.MemberTier == "Bạc");

    public int GoldCount => Customers.Count(x => x.MemberTier == "Vàng");

    public int PlatinumCount => Customers.Count(x => x.MemberTier == "Bạch Kim");
}

public class AdminCustomerManagementItemViewModel
{
    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string IdentityNumber { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public string BirthDateLabel { get; set; } = string.Empty;

    public int BookingCount { get; set; }

    public decimal TotalSpend { get; set; }

    public string TotalSpendLabel { get; set; } = string.Empty;

    public string LastBookingLabel { get; set; } = string.Empty;

    public long LastBookingSortValue { get; set; }

    public string MemberTier { get; set; } = string.Empty;

    public int MemberTierRank { get; set; }

    public string MemberTierClass { get; set; } = string.Empty;

    public string MemberTierStyle { get; set; } = string.Empty;

    public string Initials { get; set; } = "KH";

    public List<AdminCustomerBookingHistoryItemViewModel> BookingHistory { get; set; } = new();
}

public class AdminCustomerBookingHistoryItemViewModel
{
    public string BookingCode { get; set; } = string.Empty;

    public string BookingDateLabel { get; set; } = string.Empty;

    public string CheckInLabel { get; set; } = string.Empty;

    public string CheckOutLabel { get; set; } = string.Empty;

    public string RoomCodes { get; set; } = string.Empty;

    public string TotalLabel { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;
}

public class AdminCustomerUpdateRequest
{
    public string MaKh { get; set; } = string.Empty;

    public string HoTen { get; set; } = string.Empty;

    public string? GioiTinh { get; set; }

    public string? Cccd { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? QuocTich { get; set; }
}
