using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    public interface IMoMoService
    {
        Task<string> CreatePaymentUrl(string orderId, long amount, string orderInfo);
        MoMoCallbackResult ValidateCallback(Dictionary<string, string> queryParams);
        bool VerifySignature(Dictionary<string, string> data, string signature);
    }
}

