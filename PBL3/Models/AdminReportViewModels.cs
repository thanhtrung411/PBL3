namespace PBL3.Models;

public class AdminReportViewModel
{
    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public string PeriodLabel { get; set; } = string.Empty;

    public string SelectedMonthValue { get; set; } = string.Empty;

    public decimal TotalRevenue { get; set; }

    public int TotalBookings { get; set; }

    public double OccupancyRate { get; set; }

    public decimal AverageOrderValue { get; set; }

    public IReadOnlyList<string> RevenueTrendLabels { get; set; } = Array.Empty<string>();

    public IReadOnlyList<decimal> RevenueTrendValues { get; set; } = Array.Empty<decimal>();

    public IReadOnlyList<string> RoomTypeRevenueLabels { get; set; } = Array.Empty<string>();

    public IReadOnlyList<decimal> RoomTypeRevenueValues { get; set; } = Array.Empty<decimal>();

    public IReadOnlyList<string> OccupancyLabels { get; set; } = Array.Empty<string>();

    public IReadOnlyList<double> OccupancyValues { get; set; } = Array.Empty<double>();
}
