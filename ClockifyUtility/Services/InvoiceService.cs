using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IClockifyService _clockifyService;
        private readonly IFileService _fileService;
        public InvoiceService(IClockifyService clockifyService, IFileService fileService)
        {
            _clockifyService = clockifyService;
            _fileService = fileService;
        }

        public async Task<string> GenerateInvoiceAsync(DateTime start, DateTime end, ConfigModel config, Action<string>? log = null)
        {
            var entries = await _clockifyService.FetchTimeEntriesAsync(start, end, config, log);
            log?.Invoke($"[InvoiceService] Fetched {entries.Count} time entries from Clockify.");

            // Summarize by project
            var projectGroups = entries
                .GroupBy(e => e.ProjectName)
                .Select(g => new
                {
                    Project = g.Key,
                    Hours = g.Sum(e => e.Hours),
                    Descriptions = string.Join(", ", g.Select(e => e.Description).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct())
                })
                .ToList();

            double totalHours = projectGroups.Sum(g => g.Hours);
            double totalAmount = totalHours * config.HourlyRate;
            string monthYear = start.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
            var html = BuildHtmlInvoice(projectGroups.Cast<object>().ToList(), config, monthYear, totalHours, totalAmount);
            string filePath = System.IO.Path.Combine(config.OutputPath, $"Invoice_{monthYear.Replace(" ", "_")}.html");
            await _fileService.SaveHtmlAsync(html, filePath);
            return filePath;
        }

        private string BuildHtmlInvoice(List<dynamic> projectGroups, ConfigModel config, string monthYear, double totalHours, double totalAmount)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><title>Invoice</title></head><body>");
            sb.AppendLine($"<h1>Developer Invoice - {monthYear}</h1>");
            sb.AppendLine($"<h2>From: {config.FromName}</h2>");
            sb.AppendLine($"<p>{config.CompanyAddressLine1}<br>{config.CompanyAddressLine2}<br>{config.CompanyAddressLine3}</p>");
            sb.AppendLine($"<p>Email: {config.ContactEmail}<br>Phone: {config.ContactPhone}</p>");
            sb.AppendLine($"<h2>Bill To: {config.ClientName}</h2>");
            sb.AppendLine($"<p>{config.ClientAddress1}<br>{config.ClientAddress2}<br>{config.ClientAddress3}</p>");
            sb.AppendLine($"<p>Email: {config.ClientEmailAddress}<br>Phone: {config.ClientNumber}</p>");
            sb.AppendLine("<h2>Work Summary</h2>");
            sb.AppendLine("<table border='1' cellpadding='5'><tr><th>Project</th><th>Description(s)</th><th>Hours</th></tr>");
            foreach (var group in projectGroups)
            {
                sb.AppendLine($"<tr><td>{group.Project}</td><td>{group.Descriptions}</td><td>{group.Hours:F2}</td></tr>");
            }
            sb.AppendLine($"<tr><td colspan='2'><b>Total</b></td><td><b>{totalHours:F2}</b></td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine($"<h2>Amount Due: {config.CurrencySymbol}{totalAmount:F2}</h2>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }
}
