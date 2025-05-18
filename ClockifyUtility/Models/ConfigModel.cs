namespace ClockifyUtility.Models
{
	public class ConfigModel
	{
		public string? BankAccountHolder { get; set; }
		public string? BankAccountNumber { get; set; }
		public string? BankName { get; set; }
		public string? BankRoutingNumber { get; set; }
		public string? BankSwift { get; set; }
		public string? ClientAddress1 { get; set; }
		public string? ClientAddress2 { get; set; }
		public string? ClientAddress3 { get; set; }
		public string? ClientEmailAddress { get; set; }
		public string? ClientName { get; set; }
		public string? ClientNumber { get; set; }
		public string? ClockifyApiKey { get; set; }
		public string? CompanyAddressLine1 { get; set; }
		public string? CompanyAddressLine2 { get; set; }
		public string? CompanyAddressLine3 { get; set; }
		public List<ConstantLineItem> ConstantLineItems { get; set; } = [ ];
		public string? ContactEmail { get; set; }
		public string? ContactPhone { get; set; }
		public string CurrencySymbol { get; set; } = "$";
		public string? FromName { get; set; }
		public double HourlyRate { get; set; }

		// Styling properties for invoice
		public string? InvoiceNumber { get; set; }

		public InvoiceStyleConfig InvoiceStyle { get; set; } = new InvoiceStyleConfig ( );
		public string OutputPath { get; set; } = "output";
		public string? UserId { get; set; }
		public string? WorkspaceId { get; set; }

		// New font and map mode settings
		public string? InvoiceFontFamily { get; set; } = "Segoe UI, Arial, sans-serif";
		public string? InvoiceFontWeight { get; set; } = "400";
		public bool InvoiceMapMode { get; set; } = false;
	}

	public class InvoiceStyleConfig
	{
		public string AccentColor { get; set; } = "#27AE60";
		public string BackgroundColor { get; set; } = "#F4F8FB";
		public string PrimaryColor { get; set; } = "#2C3E50";
		public string SecondaryColor { get; set; } = "#2980B9";
		public string SoftAltRowBg { get; set; } = "#f6fbff";
		public string SoftHeadingBg { get; set; } = "#e3f0fa";
		public string TableBorder { get; set; } = "#BFC9D1";
		public string TableHeaderBg { get; set; } = "#EAF1FB";
		public string TextColor { get; set; } = "#222222";

		// New for more visual distinctness
		public string SectionBg { get; set; } = "#ffffff";
		public string SectionText { get; set; } = "#181818";
	}
}