using PBL3.Models;

namespace PBL3.Services.Interfaces;

public interface IBookingEmailService
{
    Task SendPaymentSuccessEmailAsync(
        PaymentCallbackResult paymentResult,
        CancellationToken cancellationToken = default);

    Task<bool> SendCheckoutReceiptEmailAsync(
        string bookingCode,
        CancellationToken cancellationToken = default);
}
