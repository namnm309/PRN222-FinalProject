using System.ComponentModel.DataAnnotations.Schema;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entities
{
	[Table("tbl_customer")]
	public class Customer : BaseEntity
	{
		public string Name { get; set; } = string.Empty;
		public CustomerType Type { get; set; } = CustomerType.Individual;
		public string? ContactEmail { get; set; }
		public string? Phone { get; set; }
		public string? Address { get; set; }
		public string? TaxId { get; set; }
	}
}
