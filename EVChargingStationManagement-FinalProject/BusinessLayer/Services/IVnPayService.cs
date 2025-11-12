using BusinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BusinessLayer.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentTransaction payment, string returnUrl, string ipAddress);
        VnPayCallbackResult ValidateCallback(Dictionary<string, string> queryParams);
        bool VerifySignature(Dictionary<string, string> data, string signature);
    }
}

