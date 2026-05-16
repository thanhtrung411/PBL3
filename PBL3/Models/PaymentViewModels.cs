using System.ComponentModel.DataAnnotations;

namespace PBL3.Models;

public class VnPayOptions
{
    public bool Enabled { get; set; } = true;
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string TmnCode { get; set; } = "";
    public string HashSecret { get; set; } = "";
    public string Version { get; set; } = "2.1.0";
    public string Command { get; set; } = "pay";
    public string CurrencyCode { get; set; } = "VND";
    public string Locale { get; set; } = "vn";
    public string OrderType { get; set; } = "other";
    public int ExpireMinutes { get; set; } = 15;
}

public class VnPayPaymentRequest
{
    [Required]
    public string BookingCode { get; set; } = "";
    public decimal Amount { get; set; }
    public string OrderInfo { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
}

public class PaymentStartResult
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PaymentCallbackResult
{
    public bool IsValidSignature { get; set; }
    public bool Success { get; set; }
    public string IpnResponseCode { get; set; } = "00";
    public string IpnMessage { get; set; } = "Confirm success";
    public string BookingCode { get; set; } = "";
    public string ResponseCode { get; set; } = "";
    public string TransactionStatus { get; set; } = "";
    public string? TransactionNo { get; set; }
    public string? BankCode { get; set; }
    public decimal Amount { get; set; }
    public string Message { get; set; } = "";
}
