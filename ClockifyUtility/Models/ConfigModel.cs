namespace ClockifyUtility.Models
{
    public class ConfigModel
    {
        public string ClockifyApiKey { get; set; }
        public string UserId { get; set; }
        public string WorkspaceId { get; set; }
        public string FromName { get; set; }
        public string CompanyAddressLine1 { get; set; }
        public string CompanyAddressLine2 { get; set; }
        public string CompanyAddressLine3 { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BankAccountHolder { get; set; }
        public string BankRoutingNumber { get; set; }
        public string BankSwift { get; set; }
        public string ClientName { get; set; }
        public string ClientAddress1 { get; set; }
        public string ClientAddress2 { get; set; }
        public string ClientAddress3 { get; set; }
        public string ClientEmailAddress { get; set; }
        public string ClientNumber { get; set; }
        public string CurrencySymbol { get; set; } = "$";
        public double HourlyRate { get; set; }
        public string OutputPath { get; set; } = "output";
        public List<ConstantLineItem> ConstantLineItems { get; set; } = new();
    }
}
