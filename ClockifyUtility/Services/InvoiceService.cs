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
        private readonly ProjectService _projectService;
        public InvoiceService(IClockifyService clockifyService, IFileService fileService, ProjectService projectService)
        {
            _clockifyService = clockifyService;
            _fileService = fileService;
            _projectService = projectService;
        }

        public async Task<string> GenerateInvoiceAsync(DateTime start, DateTime end, ConfigModel config, Action<string>? log = null)
        {
            var entries = await _clockifyService.FetchTimeEntriesAsync(start, end, config, log);
            log?.Invoke($"[InvoiceService] Fetched {entries.Count} time entries from Clockify.");

            // Use per-project API lookup with cache, log all project IDs and queries
            var projectNameCache = new ProjectNameCache(config.ClockifyApiKey, config.WorkspaceId);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                log?.Invoke($"[InvoiceService] TimeEntry {i}: projectId={entry.ProjectId}");
                if (string.IsNullOrEmpty(entry.ProjectName))
                {
                    entry.ProjectName = await projectNameCache.GetProjectNameAsync(entry.ProjectId, log);
                }
                if (string.IsNullOrEmpty(entry.ProjectName))
                {
                    entry.ProjectName = "No Project";
                }
            }

            // Summarize by resolved project name, using duration for hours, rounding only at the end
            var projectGroups = entries
                .GroupBy(e => e.ProjectName)
                .Select(g => new
                {
                    Project = g.Key,
                    Hours = g.Sum(e => e.Hours)
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
            var style = config.InvoiceStyle ?? new ClockifyUtility.Models.InvoiceStyleConfig();
            var sb = new StringBuilder();
            sb.AppendLine($"<html><head><title>Invoice</title>"
                + "<style>:root {{"
                + $"--primary: {style.PrimaryColor};"
                + $"--secondary: {style.SecondaryColor};"
                + $"--accent: {style.AccentColor};"
                + $"--background: {style.BackgroundColor};"
                + $"--text: {style.TextColor};"
                + $"--table-header-bg: {style.TableHeaderBg};"
                + $"--table-border: {style.TableBorder};"
                + "}}\n"
                + "body { background: var(--background); color: var(--text); font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 2em; }\n"
                + "h1, h2 { color: var(--primary); }\n"
                + "table { width: 100%; border-collapse: collapse; margin-bottom: 2em; }\n"
                + "th, td { border: 1px solid var(--table-border); padding: 10px 8px; }\n"
                + "th { background: var(--table-header-bg); color: var(--primary); }\n"
                + "tr:nth-child(even) { background: #f9fbfd; }\n"
                + ".right { text-align: right; }\n"
                + ".desc { font-size: 0.95em; color: #555; }"
                + "</style></head><body>");
            sb.AppendLine($"<h1>Developer Invoice - {monthYear}</h1>");
            sb.AppendLine($"<h2>From: {config.FromName}</h2>");
            sb.AppendLine($"<p>{config.CompanyAddressLine1}<br>{config.CompanyAddressLine2}<br>{config.CompanyAddressLine3}</p>");
            sb.AppendLine($"<p>Email: {config.ContactEmail}<br>Phone: {config.ContactPhone}</p>");
            sb.AppendLine($"<h2>Bill To: {config.ClientName}</h2>");
            sb.AppendLine($"<p>{config.ClientAddress1}<br>{config.ClientAddress2}<br>{config.ClientAddress3}</p>");
            sb.AppendLine($"<p>Email: {config.ClientEmailAddress}<br>Phone: {config.ClientNumber}</p>");
            sb.AppendLine("<h2>Work Summary</h2>");
            sb.AppendLine("<table><tr><th>Project</th><th class='right'>Hours</th><th class='right'>Rate</th><th class='right'>Amount</th></tr>");

            double totalAmountTable = 0;
            double totalHoursTable = 0;
            foreach (var group in projectGroups)
            {
                double amount = group.Hours * config.HourlyRate;
                sb.AppendLine($"<tr><td>{group.Project}</td><td class='right'>{group.Hours:F2}</td><td class='right'>{config.CurrencySymbol}{config.HourlyRate:F2}</td><td class='right'>{config.CurrencySymbol}{amount:F2}</td></tr>");
                totalAmountTable += amount;
                totalHoursTable += group.Hours;
            }

            // Constant line items
            if (config.ConstantLineItems != null)
            {
                foreach (var item in config.ConstantLineItems)
                {
                    sb.AppendLine($"<tr><td>{item.Description}</td><td class='right'></td><td class='right'></td><td class='right'>{config.CurrencySymbol}{item.Amount:F2}</td></tr>");
                    totalAmountTable += item.Amount;
                }
            }

            sb.AppendLine($"<tr><td><b>Total</b></td><td class='right'><b>{totalHoursTable:F2}</b></td><td class='right'></td><td class='right'><b>{config.CurrencySymbol}{totalAmountTable:F2}</b></td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine($"<h2>Amount Due: {config.CurrencySymbol}{totalAmountTable:F2}</h2>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }
}
