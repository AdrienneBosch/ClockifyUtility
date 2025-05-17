using System.Collections.Generic;

namespace ClockifyUtility.Models
{
    public class InvoiceModel
    {
        public string MonthYear { get; set; }
        public string FromName { get; set; }
        public List<string> CompanyAddressLines { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ClientName { get; set; }
        public List<string> ClientAddressLines { get; set; }
        public string ClientEmail { get; set; }
        public string ClientNumber { get; set; }
        public List<TimeEntryModel> TimeEntries { get; set; }
        public double HourlyRate { get; set; }
        public string CurrencySymbol { get; set; }
        public double TotalHours { get; set; }
        public double TotalAmount { get; set; }
    }
}
