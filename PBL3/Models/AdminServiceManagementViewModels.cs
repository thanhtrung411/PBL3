namespace PBL3.Models;

public class AdminServiceManagementViewModel
{
    public List<AdminServiceManagementItemViewModel> Services { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public int TotalCount => Services.Count;

    public int ActiveCount => Services.Count(x => x.IsActive);

    public int MonthlyUsageCount { get; set; }

    public decimal MonthlyRevenue { get; set; }
}

public class AdminServiceManagementItemViewModel
{
    public string ServiceId { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string PriceLabel { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string Description { get; set; } = string.Empty;

    public string IconClass { get; set; } = "bi-gear";

    public int MonthlyUsageCount { get; set; }

    public string MonthlyRevenueLabel { get; set; } = string.Empty;
}
