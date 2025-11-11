namespace BusinessLayer.DTOs
{
    public class CreateVnPayPaymentRequest
    {
        public Guid SessionId { get; set; }
        public decimal Amount { get; set; }
        public string? ReturnUrl { get; set; }
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
}

