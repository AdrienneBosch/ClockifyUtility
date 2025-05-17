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
                + $"--soft-heading-bg: {style.SoftHeadingBg};"
                + $"--soft-alt-row-bg: {style.SoftAltRowBg};"
                + "}}\n"
                + "body { background: var(--background); color: var(--text); font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 2em; min-height: 100vh; }\n"
                + ".invoice-header { background: var(--soft-heading-bg); border-radius: 10px; padding: 1.5em 2em 1em 2em; margin-bottom: 2em; box-shadow: 0 2px 8px rgba(44,62,80,0.07); }\n"
                + ".invoice-title-row { display: flex; justify-content: space-between; align-items: flex-end; flex-wrap: wrap; margin-bottom: 1em; }\n"
                + ".invoice-title { font-size: 2em; color: var(--primary); font-weight: 600; }\n"
                + ".invoice-meta { font-size: 1.1em; color: var(--secondary); text-align: right; }\n"
                + ".parties-row { display: flex; justify-content: space-between; gap: 2em; flex-wrap: wrap; }\n"
                + ".party { width: 48%; min-width: 220px; }\n"
                + ".party h3 { margin: 0 0 0.3em 0; color: var(--primary); font-size: 1.1em; }\n"
                + ".party p { margin: 0.1em 0; }\n"
                + "h2 { color: var(--secondary); font-size: 1.3em; margin-top: 2em; margin-bottom: 0.7em; }\n"
                + "table { width: 100%; border-collapse: collapse; margin-bottom: 2em; box-shadow: 0 2px 8px rgba(44,62,80,0.07); background: #fff; }\n"
                + "th, td { border: 1px solid var(--table-border); padding: 12px 10px; font-size: 1em; }\n"
                + "th { background: var(--table-header-bg); color: var(--primary); letter-spacing: 0.03em; }\n"
                + "tr:nth-child(even) { background: var(--soft-alt-row-bg); }\n"
                + "tr:hover { background: var(--accent); color: #fff; transition: background 0.2s, color 0.2s; }\n"
                + ".right { text-align: right; }\n"
                + ".desc { font-size: 0.97em; color: var(--secondary); }\n"
                + ".amount-due { background: var(--primary); color: #fff; padding: 1em 2em; border-radius: 8px; display: inline-block; font-size: 1.3em; margin-top: 1.5em; }\n"
                + "@media (max-width: 900px) { .parties-row { flex-direction: column; } .party { width: 100%; } .invoice-title-row { flex-direction: column; align-items: flex-start; } .invoice-meta { text-align: left; margin-top: 0.5em; } }\n"
                + "@media (max-width: 700px) { .invoice-header { padding: 1em 0.5em; } .invoice-title { font-size: 1.2em; } table, th, td { font-size: 0.95em; } }"
                + "</style></head><body>");

            // Header section with invoice number and dates
            sb.AppendLine("<div class='invoice-header'>");
            sb.AppendLine("  <div class='invoice-title-row'>");
            sb.AppendLine("    <div class='invoice-title'>Developer Invoice</div>");
            sb.AppendLine("    <div class='invoice-meta'>");
            if (!string.IsNullOrWhiteSpace(config.InvoiceNumber))
                sb.AppendLine($"<div><b>Invoice #:</b> {config.InvoiceNumber}</div>");
            sb.AppendLine($"<div><b>Date:</b> {DateTime.Now:yyyy-MM-dd}</div>");
            sb.AppendLine($"<div><b>Period:</b> {monthYear}</div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class='parties-row'>");
            sb.AppendLine("    <div class='party'>");
            sb.AppendLine("      <h3>From:</h3>");
            sb.AppendLine($"      <p>{config.FromName}</p>");
            sb.AppendLine($"      <p>{config.CompanyAddressLine1}<br>{config.CompanyAddressLine2}<br>{config.CompanyAddressLine3}</p>");
            sb.AppendLine($"      <p>Email: {config.ContactEmail}<br>Phone: {config.ContactPhone}</p>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div class='party'>");
            sb.AppendLine("      <h3>Bill To:</h3>");
            sb.AppendLine($"      <p>{config.ClientName}</p>");
            sb.AppendLine($"      <p>{config.ClientAddress1}<br>{config.ClientAddress2}<br>{config.ClientAddress3}</p>");
            sb.AppendLine($"      <p>Email: {config.ClientEmailAddress}<br>Phone: {config.ClientNumber}</p>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<h2>Work Summary</h2>");
            sb.AppendLine("<table><tr><th>Project</th><th class='right'>Hours</th><th class='right'>Rate</th><th class='right'>Amount</th></tr>");
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
