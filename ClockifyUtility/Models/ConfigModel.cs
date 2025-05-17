namespace ClockifyUtility.Models
{
    public class ConfigModel
    {
        public string? ClockifyApiKey { get; set; }
        public string? UserId { get; set; }
        public string? WorkspaceId { get; set; }
        public string? FromName { get; set; }
        public string? CompanyAddressLine1 { get; set; }
        public string? CompanyAddressLine2 { get; set; }
        public string? CompanyAddressLine3 { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolder { get; set; }
        public string? BankRoutingNumber { get; set; }
        public string? BankSwift { get; set; }
        public string? ClientName { get; set; }
        public string? ClientAddress1 { get; set; }
        public string? ClientAddress2 { get; set; }
        public string? ClientAddress3 { get; set; }
        public string? ClientEmailAddress { get; set; }
        public string? ClientNumber { get; set; }
        public string CurrencySymbol { get; set; } = "$";
        public double HourlyRate { get; set; }
        public string OutputPath { get; set; } = "output";
        public List<ConstantLineItem> ConstantLineItems { get; set; } = new();

        // Styling properties for invoice
        public string? InvoiceNumber { get; set; }
        public InvoiceStyleConfig InvoiceStyle { get; set; } = new InvoiceStyleConfig();
    }

    public class InvoiceStyleConfig
    {
        public string PrimaryColor { get; set; } = "#2C3E50";
        public string SecondaryColor { get; set; } = "#2980B9";
        public string AccentColor { get; set; } = "#27AE60";
        public string BackgroundColor { get; set; } = "#F4F8FB";
        public string TextColor { get; set; } = "#222222";
        public string TableHeaderBg { get; set; } = "#EAF1FB";
        public string TableBorder { get; set; } = "#BFC9D1";
        public string SoftHeadingBg { get; set; } = "#e3f0fa";
        public string SoftAltRowBg { get; set; } = "#f6fbff";
    }
}
