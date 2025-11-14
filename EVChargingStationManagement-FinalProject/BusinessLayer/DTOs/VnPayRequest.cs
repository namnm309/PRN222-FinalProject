using System.Text.Json.Serialization;

namespace BusinessLayer.DTOs
{
    public class CreateVnPayPaymentRequest
    {
        [JsonPropertyName("sessionId")]
        public Guid? SessionId { get; set; }
        
        [JsonPropertyName("reservationId")]
        public Guid? ReservationId { get; set; }
        
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
        
        [JsonPropertyName("returnUrl")]
        public string? ReturnUrl { get; set; }
    }

    public class CreateCashPaymentRequest
    {
        [JsonPropertyName("sessionId")]
        public Guid SessionId { get; set; }
        
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class VnPayCallbackResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? ResponseCode { get; set; }
        public string? Message { get; set; }
        public string? OrderId { get; set; }
        public string? TransactionNo { get; set; }
    }

    public class CreateMoMoPaymentRequest
    {
        [JsonPropertyName("sessionId")]
        public Guid SessionId { get; set; }
        
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }


    public class MoMoCallbackResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? ResponseCode { get; set; }
        public string? Message { get; set; }
        public string? OrderId { get; set; }
        public string? TransactionNo { get; set; }
        public string? PartnerCode { get; set; }
        public string? RequestId { get; set; }
    }
}

