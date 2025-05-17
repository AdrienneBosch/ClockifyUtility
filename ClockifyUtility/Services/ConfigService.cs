using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
    public class ConfigService : IConfigService
    {
        public ConfigModel LoadConfig()
        {
            // TODO: Load from file or environment. For now, return dummy config.
            return new ConfigModel
            {
                ClockifyApiKey = "dummy-key",
                UserId = "dummy-user",
                WorkspaceId = "dummy-workspace",
                FromName = "Your Name",
                CompanyAddressLine1 = "123 Main St",
                CompanyAddressLine2 = "Suite 456",
                CompanyAddressLine3 = "City, State ZIP",
                ContactEmail = "you@example.com",
                ContactPhone = "+1-555-123-4567",
                BankName = "Your Bank",
                BankAccountNumber = "000123456789",
                BankAccountHolder = "Your Name",
                BankRoutingNumber = "111000025",
                BankSwift = "BOFAUS3NXXX",
                ClientName = "Client Name",
                ClientAddress1 = "456 Client Rd",
                ClientAddress2 = "",
                ClientAddress3 = "",
                ClientEmailAddress = "client@example.com",
                ClientNumber = "+11234567890",
                CurrencySymbol = "$",
                HourlyRate = 100.0,
                OutputPath = "output"
            };
        }
    }
}
