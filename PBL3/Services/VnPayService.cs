using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PBL3.Data;
using PBL3.Models;
using PBL3.Services.Interfaces;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace PBL3.Services;

public class VnPayService : IVnPayService
{
    private readonly ApplicationDbContext _context;
    private readonly VnPayOptions _options;

    public VnPayService(ApplicationDbContext context, IOptions<VnPayOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public PaymentStartResult CreatePaymentUrl(VnPayPaymentRequest request)
    {
        if (!_options.Enabled)
        {
            return Fail("Cổng thanh toán VNPay hiện chưa được bật.");
        }

        if (string.IsNullOrWhiteSpace(_options.PaymentUrl) ||
            string.IsNullOrWhiteSpace(_options.TmnCode) ||
            string.IsNullOrWhiteSpace(_options.HashSecret))
        {
            return Fail("Thiếu cấu hình VNPay. Vui lòng cấu hình Payment:VnPay:TmnCode và Payment:VnPay:HashSecret.");
        }

        if (request.Amount <= 0)
        {
            return Fail("Số tiền thanh toán không hợp lệ.");
        }

        var now = GetVietnamTime();
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _options.Version,
            ["vnp_Command"] = _options.Command,
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Amount"] = ((long)(request.Amount * 100)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = FormatVnPayDate(now),
            ["vnp_CurrCode"] = _options.CurrencyCode,
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(request.IpAddress) ? "127.0.0.1" : request.IpAddress,
            ["vnp_Locale"] = _options.Locale,
            ["vnp_OrderInfo"] = string.IsNullOrWhiteSpace(request.OrderInfo) ? $"Thanh toan dat phong {request.BookingCode}" : request.OrderInfo,
            ["vnp_OrderType"] = _options.OrderType,
            ["vnp_ReturnUrl"] = request.ReturnUrl,
            ["vnp_TxnRef"] = request.BookingCode.Trim()
        };

        if (_options.ExpireMinutes > 0)
        {
            parameters["vnp_ExpireDate"] = FormatVnPayDate(now.AddMinutes(_options.ExpireMinutes));
        }

        var query = BuildQuery(parameters, encodeValues: true);
        var hashData = BuildQuery(parameters, encodeValues: true);
        var secureHash = HmacSha512(_options.HashSecret, hashData);
        return new PaymentStartResult
        {
            Success = true,
            PaymentUrl = $"{_options.PaymentUrl}?{query}&vnp_SecureHash={secureHash}"
        };
    }

    public PaymentCallbackResult ReadCallback(IQueryCollection query)
    {
        var data = query
            .Where(x => x.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.Ordinal);

        var receivedHash = query.TryGetValue("vnp_SecureHash", out var hashValues)
            ? hashValues.ToString()
            : string.Empty;
        var hashData = BuildQuery(new SortedDictionary<string, string>(data, StringComparer.Ordinal), encodeValues: true);
        var expectedHash = string.IsNullOrWhiteSpace(_options.HashSecret)
            ? string.Empty
            : HmacSha512(_options.HashSecret, hashData);
        var isValid = !string.IsNullOrWhiteSpace(receivedHash) &&
                      CryptographicOperations.FixedTimeEquals(
                          Encoding.UTF8.GetBytes(receivedHash.ToUpperInvariant()),
                          Encoding.UTF8.GetBytes(expectedHash.ToUpperInvariant()));

        var amount = 0m;
        if (data.TryGetValue("vnp_Amount", out var amountText) &&
            decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount))
        {
            amount = parsedAmount / 100m;
        }

        var responseCode = data.GetValueOrDefault("vnp_ResponseCode", "");
        var transactionStatus = data.GetValueOrDefault("vnp_TransactionStatus", "");
        var success = isValid && responseCode == "00" && transactionStatus == "00";

        return new PaymentCallbackResult
        {
            IsValidSignature = isValid,
            Success = success,
            IpnResponseCode = isValid ? "00" : "97",
            IpnMessage = isValid ? "Confirm success" : "Invalid signature",
            BookingCode = data.GetValueOrDefault("vnp_TxnRef", "").Trim(),
            ResponseCode = responseCode,
            TransactionStatus = transactionStatus,
            TransactionNo = data.GetValueOrDefault("vnp_TransactionNo"),
            BankCode = data.GetValueOrDefault("vnp_BankCode"),
            Amount = amount,
            Message = success ? "Thanh toán thành công." : "Thanh toán không thành công hoặc đã bị hủy."
        };
    }

    public async Task<PaymentCallbackResult> ProcessCallbackAsync(IQueryCollection query)
    {
        var result = ReadCallback(query);
        if (!result.IsValidSignature || string.IsNullOrWhiteSpace(result.BookingCode))
        {
            result.Message = "Chữ ký thanh toán không hợp lệ.";
            result.IpnResponseCode = result.IsValidSignature ? "01" : "97";
            result.IpnMessage = result.IsValidSignature ? "Order not found" : "Invalid signature";
            return result;
        }

        var invoice = await _context.HoaDons
            .Include(x => x.MaDatPhongNavigation)
            .FirstOrDefaultAsync(x => x.MaDatPhong == result.BookingCode);
        if (invoice == null)
        {
            result.Message = "Không tìm thấy hóa đơn tương ứng.";
            result.IpnResponseCode = "01";
            result.IpnMessage = "Order not found";
            return result;
        }

        if (invoice.TrangThai == DomainValues.HoaDonTrangThai.DaHuy ||
            invoice.MaDatPhongNavigation.TrangThai == DomainValues.DatPhongTrangThai.DaHuy)
        {
            result.Success = false;
            result.Message = "Đơn đặt phòng đã quá hạn thanh toán và đã được hủy.";
            result.IpnResponseCode = "02";
            result.IpnMessage = "Order already processed";
            return result;
        }

        if (result.Success && invoice.TrangThai == DomainValues.HoaDonTrangThai.DaThanhToan)
        {
            result.IpnResponseCode = "02";
            result.IpnMessage = "Order already confirmed";
            return result;
        }

        if (result.Success)
        {
            if (invoice.TongThanhToan > 0 && result.Amount != invoice.TongThanhToan)
            {
                result.Success = false;
                result.Message = "Số tiền thanh toán không khớp với hóa đơn.";
                result.IpnResponseCode = "04";
                result.IpnMessage = "Invalid amount";
                return result;
            }

            invoice.SoTienDaThanhToan = invoice.TongThanhToan;
            invoice.TienDatCoc = invoice.TongThanhToan;
            invoice.NgayThanhToanCuoi = DateTime.Now;
            invoice.PhuongThucThanhToan = DomainValues.PhuongThucThanhToan.Qr;
            invoice.TrangThai = DomainValues.HoaDonTrangThai.DaThanhToan;
            invoice.GhiChu = AppendNote(invoice.GhiChu, $"VNPay: {result.TransactionNo}; Bank: {result.BankCode}");
            await _context.SaveChangesAsync();
        }

        result.IpnResponseCode = "00";
        result.IpnMessage = "Confirm success";

        return result;
    }

    private static PaymentStartResult Fail(string message)
    {
        return new PaymentStartResult { Success = false, ErrorMessage = message };
    }

    private static string BuildQuery(SortedDictionary<string, string> parameters, bool encodeValues)
    {
        return string.Join("&", parameters
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{WebUtility.UrlEncode(x.Key)}={(encodeValues ? WebUtility.UrlEncode(x.Value) : x.Value)}"));
    }

    private static string HmacSha512(string key, string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        using var hmac = new HMACSHA512(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(inputBytes)).ToLowerInvariant();
    }

    private static DateTime GetVietnamTime()
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }
    }

    private static string FormatVnPayDate(DateTime value)
    {
        return value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
    }

    private static string AppendNote(string? current, string next)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return next.Length <= 255 ? next : next[..255];
        }

        var combined = $"{current}; {next}";
        return combined.Length <= 255 ? combined : combined[..255];
    }
}
