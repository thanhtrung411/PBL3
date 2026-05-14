using PBL3.Models;

namespace PBL3.Services.Interfaces;

public interface IVnPayService
{
    PaymentStartResult CreatePaymentUrl(VnPayPaymentRequest request);
    PaymentCallbackResult ReadCallback(IQueryCollection query);
    Task<PaymentCallbackResult> ProcessCallbackAsync(IQueryCollection query);
}
