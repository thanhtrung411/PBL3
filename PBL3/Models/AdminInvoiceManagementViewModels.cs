namespace PBL3.Models;

public class AdminInvoiceManagementViewModel
{
    public List<AdminInvoiceManagementItemViewModel> Invoices { get; set; } = new();

    public List<AdminInvoiceEmployeeFilterOptionViewModel> Employees { get; set; } = new();

    public decimal TotalPaid { get; set; }

    public decimal TotalRemaining { get; set; }

    public decimal OverdueRemaining { get; set; }

    public decimal TotalAmount { get; set; }
}

public class AdminInvoiceManagementItemViewModel
{
    public string InvoiceCode { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string EmployeeId { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeePosition { get; set; } = string.Empty;

    public string RoomSummary { get; set; } = string.Empty;

    public string CreatedDateLabel { get; set; } = string.Empty;

    public string CheckInLabel { get; set; } = string.Empty;

    public string CheckOutLabel { get; set; } = string.Empty;

    public string SortDateValue { get; set; } = string.Empty;

    public string RoomAmountLabel { get; set; } = string.Empty;

    public string ServiceAmountLabel { get; set; } = string.Empty;

    public string DiscountAmountLabel { get; set; } = string.Empty;

    public string TotalAmountLabel { get; set; } = string.Empty;

    public string PaidAmountLabel { get; set; } = string.Empty;

    public string RemainingAmountLabel { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaidDateLabel { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public string StatusClass { get; set; } = string.Empty;

    public bool IsOverdue { get; set; }

    public List<AdminInvoiceDetailLineItemViewModel> LineItems { get; set; } = new();
}

public class AdminInvoiceEmployeeFilterOptionViewModel
{
    public string EmployeeId { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;
}

public class AdminInvoiceDetailLineItemViewModel
{
    public string TypeLabel { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RoomCode { get; set; } = string.Empty;

    public string DateLabel { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string UnitPriceLabel { get; set; } = string.Empty;

    public string AmountLabel { get; set; } = string.Empty;
}
