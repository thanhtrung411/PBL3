namespace PBL3.Models;

public sealed class AdminDashboardViewModel
{
    public int TotalRooms { get; set; }

    public int OperationalRooms { get; set; }

    public int OccupiedRooms { get; set; }

    public decimal TodayRevenue { get; set; }

    public double OccupancyRate { get; set; }

    public DateTime GeneratedAt { get; set; }

    public IReadOnlyList<string> MonthlyRevenueLabels { get; set; } = Array.Empty<string>();

    public IReadOnlyList<decimal> MonthlyRevenueValues { get; set; } = Array.Empty<decimal>();

    public IReadOnlyList<RoomStatusDashboardItem> RoomStatuses { get; set; } = Array.Empty<RoomStatusDashboardItem>();

    public IReadOnlyList<RecentBookingDashboardItem> RecentBookings { get; set; } = Array.Empty<RecentBookingDashboardItem>();
}

public sealed class RoomStatusDashboardItem
{
    public string Label { get; set; } = "";

    public int Count { get; set; }

    public string Color { get; set; } = "#6b7280";

    public string LegendClass { get; set; } = "text-secondary";
}

public sealed class RecentBookingDashboardItem
{
    public string BookingCode { get; set; } = "";

    public string CustomerName { get; set; } = "";

    public string? CustomerPhone { get; set; }

    public string RoomSummary { get; set; } = "";

    public DateOnly CheckInDate { get; set; }

    public int Nights { get; set; }

    public string StatusLabel { get; set; } = "";

    public string StatusClass { get; set; } = "badge-booked";
}
