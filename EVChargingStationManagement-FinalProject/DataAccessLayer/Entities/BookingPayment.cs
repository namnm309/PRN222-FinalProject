using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("tbl_booking_payment")]
    public class BookingPayment : BaseEntity
	{
       
        public Guid BookingId { get; set; }
		public Booking? Booking { get; set; }
		public long Amount { get; set; } // VND
		public string Provider { get; set; } = "VNPay";
		public string Status { get; set; } = "Pending"; // Pending, Succeeded, Failed
		public string? VnpTxnRef { get; set; }
		public string? VnpTransactionNo { get; set; }
		public string? BankCode { get; set; }
		public DateTime? PaidAt { get; set; }
	}
}


