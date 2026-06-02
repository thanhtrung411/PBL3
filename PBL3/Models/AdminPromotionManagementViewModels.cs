namespace PBL3.Models;

public class AdminPromotionManagementViewModel
{
    public List<AdminPromotionManagementItemViewModel> Promotions { get; set; } = new();

    public int TotalCount => Promotions.Count;

    public int ActiveCount => Promotions.Count(x => x.StatusKey == "active");

    public int TotalUsage => Promotions.Sum(x => x.UsedCount);

    public decimal TotalDiscountAmount { get; set; }
}

public class AdminPromotionManagementItemViewModel
{
    public string PromotionId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string DiscountLabel { get; set; } = string.Empty;

    public string MaxDiscountLabel { get; set; } = string.Empty;

    public string MinimumInvoiceLabel { get; set; } = string.Empty;

    public string ScopeLabel { get; set; } = string.Empty;

    public string DateRangeLabel { get; set; } = string.Empty;

    public string StartDateValue { get; set; } = string.Empty;

    public string EndDateValue { get; set; } = string.Empty;

    public int IssuedCount { get; set; }

    public int UsedCount { get; set; }

    public decimal UsagePercent { get; set; }

    public string StatusLabel { get; set; } = string.Empty;

    public string StatusKey { get; set; } = string.Empty;

    public string StatusIcon { get; set; } = string.Empty;

    public string StatusClass { get; set; } = string.Empty;
}
