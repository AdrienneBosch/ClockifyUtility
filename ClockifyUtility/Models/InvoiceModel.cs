namespace ClockifyUtility.Models
{
	public class InvoiceModel
	{
		public required List<string> ClientAddressLines { get; set; }
		public required string ClientEmail { get; set; }
		public required string ClientName { get; set; }
		public required string ClientNumber { get; set; }
		public required List<string> CompanyAddressLines { get; set; }
		public required string ContactEmail { get; set; }
		public required string ContactPhone { get; set; }
		public required string CurrencySymbol { get; set; }
		public required string FromName { get; set; }
		public double HourlyRate { get; set; }
		public required string MonthYear { get; set; }
		public required List<TimeEntryModel> TimeEntries { get; set; }
		public double TotalAmount { get; set; }
		public double TotalHours { get; set; }
	}
}