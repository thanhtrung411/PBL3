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
    private readonly ILogger<VnPayService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IBookingEmailService _bookingEmailService;

    public VnPayService(
        ApplicationDbContext context,
        IOptions<VnPayOptions> options,
        ILogger<VnPayService> logger,
        IWebHostEnvironment environment,
        IBookingEmailService bookingEmailService)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
        _environment = environment;
        _bookingEmailService = bookingEmailService;
    }

    public PaymentStartResult CreatePaymentUrl(VnPayPaymentRequest request)
    {
        if (!_options.Enabled)
        {
            return Fail("Cổng thanh toán VNPay hiện chưa được bật.");
        }

        var tmnCode = NormalizeSecretValue(_options.TmnCode);
        var hashSecret = NormalizeSecretValue(_options.HashSecret);
        if (string.IsNullOrWhiteSpace(_options.PaymentUrl) ||
            string.IsNullOrWhiteSpace(tmnCode) ||
            string.IsNullOrWhiteSpace(hashSecret))
        {
            return Fail("Thiếu cấu hình VNPay. Vui lòng cấu hình Payment:VnPay:TmnCode và Payment:VnPay:HashSecret.");
        }

        if (request.Amount <= 0)
        {
            return Fail("Số tiền thanh toán không hợp lệ.");
        }

        var now = GetVietnamTime();
        var parameters = new SortedList<string, string>(new VnPayCompare())
        {
            ["vnp_Version"] = _options.Version,
            ["vnp_Command"] = _options.Command,
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_Amount"] = ((long)(request.Amount * 100)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = FormatVnPayDate(now),
            ["vnp_CurrCode"] = _options.CurrencyCode,
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(request.IpAddress) ? "127.0.0.1" : request.IpAddress,
            ["vnp_Locale"] = _options.Locale,
            ["vnp_OrderInfo"] = string.IsNullOrWhiteSpace(request.OrderInfo) ? $"Thanh toan dat phong {request.BookingCode}" : request.OrderInfo,
            ["vnp_OrderType"] = string.IsNullOrWhiteSpace(_options.OrderType) ? "other" : _options.OrderType.Trim(),
            ["vnp_ReturnUrl"] = request.ReturnUrl,
            ["vnp_TxnRef"] = request.BookingCode.Trim()
        };

        if (_options.ExpireMinutes > 0)
        {
            parameters["vnp_ExpireDate"] = FormatVnPayDate(now.AddMinutes(_options.ExpireMinutes));
        }

        var (query, hashData) = BuildRequestQuery(parameters);
        var secureHash = HmacSha512(hashSecret, hashData);
        _logger.LogInformation(
            "VNPay request created. TxnRef={TxnRef}; TmnCode={TmnCode}; Amount={Amount}; ReturnUrl={ReturnUrl}; HashData={HashData}",
            request.BookingCode,
            tmnCode,
            request.Amount,
            request.ReturnUrl,
            hashData);
        WriteDebugLog(request, tmnCode, hashSecret, hashData, secureHash);

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
        var hashData = BuildResponseHashData(new SortedList<string, string>(data, new VnPayCompare()));
        var hashSecret = NormalizeSecretValue(_options.HashSecret);
        var expectedHash = string.IsNullOrWhiteSpace(hashSecret)
            ? string.Empty
            : HmacSha512(hashSecret, hashData);
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
            await SendPaymentSuccessEmailAsync(result);
        }

        result.IpnResponseCode = "00";
        result.IpnMessage = "Confirm success";

        return result;
    }

    private async Task SendPaymentSuccessEmailAsync(PaymentCallbackResult result)
    {
        try
        {
            await _bookingEmailService.SendPaymentSuccessEmailAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Payment was confirmed but the success email could not be sent. BookingCode={BookingCode}",
                result.BookingCode);
        }
    }

    private static PaymentStartResult Fail(string message)
    {
        return new PaymentStartResult { Success = false, ErrorMessage = message };
    }

    private static (string Query, string HashData) BuildRequestQuery(SortedList<string, string> parameters)
    {
        var data = new StringBuilder();
        foreach (var parameter in parameters)
        {
            if (!string.IsNullOrEmpty(parameter.Value))
            {
                data.Append(WebUtility.UrlEncode(parameter.Key));
                data.Append('=');
                data.Append(WebUtility.UrlEncode(parameter.Value));
                data.Append('&');
            }
        }

        var queryString = data.ToString();
        var hashData = queryString.Length > 0
            ? queryString.Remove(queryString.Length - 1, 1)
            : queryString;

        return (queryString, hashData);
    }

    private static string BuildResponseHashData(SortedList<string, string> parameters)
    {
        parameters.Remove("vnp_SecureHashType");
        parameters.Remove("vnp_SecureHash");

        var data = new StringBuilder();
        foreach (var parameter in parameters)
        {
            if (!string.IsNullOrEmpty(parameter.Value))
            {
                data.Append(WebUtility.UrlEncode(parameter.Key));
                data.Append('=');
                data.Append(WebUtility.UrlEncode(parameter.Value));
                data.Append('&');
            }
        }

        if (data.Length > 0)
        {
            data.Remove(data.Length - 1, 1);
        }

        return data.ToString();
    }

    private static string HmacSha512(string key, string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        using var hmac = new HMACSHA512(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(inputBytes)).ToLowerInvariant();
    }

    private static string NormalizeSecretValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(c => !char.IsWhiteSpace(c) && c != '\u200b' && c != '\ufeff')
            .ToArray());
    }

    private void WriteDebugLog(
        VnPayPaymentRequest request,
        string tmnCode,
        string hashSecret,
        string hashData,
        string secureHash)
    {
        try
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            Directory.CreateDirectory(webRootPath);
            var debugPath = Path.Combine(webRootPath, "vnpay-debug.log");
            var hashSecretFingerprint = Sha256(hashSecret)[..16];
            var lines = new[]
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] VNPay request",
                $"BookingCode={request.BookingCode}",
                $"Amount={request.Amount.ToString(CultureInfo.InvariantCulture)}",
                $"TmnCode={tmnCode}",
                $"HashSecretLength={hashSecret.Length}",
                $"HashSecretSha256Prefix={hashSecretFingerprint}",
                $"ReturnUrl={request.ReturnUrl}",
                $"HashData={hashData}",
                $"SecureHash={secureHash}",
                ""
            };

            File.AppendAllLines(debugPath, lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write VNPay debug log.");
        }
    }

    private static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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

    private sealed class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            var comparer = CompareInfo.GetCompareInfo("en-US");
            return comparer.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}
