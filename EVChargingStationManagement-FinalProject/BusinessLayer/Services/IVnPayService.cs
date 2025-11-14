using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentTransactionDTO payment, string returnUrl, string ipAddress);
        VnPayCallbackResult ValidateCallback(Dictionary<string, string> queryParams);
        bool VerifySignature(Dictionary<string, string> data, string signature);
        string GetConfiguredReturnUrl();
    }
}

