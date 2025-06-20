using System.Globalization;
using System.Text;
using ClockifyUtility.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ClockifyUtility.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IClockifyService _clockifyService;
        private readonly ProjectService _projectService;
        private readonly IPdfService _pdfService;

        public InvoiceService(
            IClockifyService clockifyService,
            IFileService fileService, // still injected for compatibility, but not used
            ProjectService projectService,
            IPdfService pdfService
        )
        {
            _clockifyService = clockifyService;
            _projectService = projectService;
            _pdfService = pdfService;
        }

		private string BuildHtmlInvoice ( List<dynamic> projectGroups, InvoiceConfig config, string monthYear, double totalHours, double totalAmount )
		{
			InvoiceStyle style = config.InvoiceStyle;
			string fontFamily = config.InvoiceFontFamily ?? "Segoe UI, Arial, sans-serif";
			string fontWeight = config.InvoiceFontWeight ?? "500";
			bool mapMode = config.InvoiceMapMode.HasValue && config.InvoiceMapMode.Value;
			string headerColor = mapMode ? "#FF0000" : style.PrimaryColor ?? "#2C3E50";
			string amountDueColor = mapMode ? "#FF0000" : style.SecondaryColor ?? "#2980B9";
			string headingBg = style.SoftHeadingBg ?? "#e3f0fa";
			string altRowBg = style.SoftAltRowBg ?? "#f6fbff";
			string borderColor = style.TableBorder ?? "#BFC9D1";
			string tableHeaderBg = style.TableHeaderBg ?? "#D0E4FA";
			string textColor = style.TextColor ?? "#181818";
			string backgroundColor = style.BackgroundColor ?? "#F4F8FB";
			string sectionBg = style.SectionBg ?? "#ffffff";
			string sectionText = style.SectionText ?? "#181818";

			StringBuilder sb = new();

			sb.AppendLine ( $"<html><body style='background:{backgroundColor};color:{textColor};font-family:{fontFamily};font-weight:{fontWeight};margin:0;padding:2em;min-height:100vh;'>" );
			sb.AppendLine ( $"<div style='max-width:730px;margin:0 auto;padding:0 2vw;background:{sectionBg};color:{sectionText};border-radius:10px;box-shadow:0 2px 8px rgba(44,62,80,0.04);'>" );

			sb.AppendLine ( $"<div style='background:{headingBg};border-radius:14px;padding:1.8em 2em 1.3em 2em;margin-bottom:2em;box-shadow:0 4px 16px rgba(44,62,80,0.09);color:{textColor};'>" );
			sb.AppendLine ( $"  <div style='display:flex;align-items:center;justify-content:space-between;flex-wrap:nowrap;width:100%;'>" );
			sb.AppendLine ( $"    <div style='display:flex;align-items:center;flex-shrink:0;'>" );
			if ( !string.IsNullOrWhiteSpace ( config.Clockify.LogoPath ) && System.IO.File.Exists ( config.Clockify.LogoPath ) )
			{
				byte[] imageBytes = System.IO.File.ReadAllBytes(config.Clockify.LogoPath);
				string base64 = Convert.ToBase64String(imageBytes);
				string ext = System.IO.Path.GetExtension(config.Clockify.LogoPath).ToLowerInvariant();
				string mimeType = ext switch { ".png"=>"image/png", ".jpg" or ".jpeg"=>"image/jpeg", ".gif"=>"image/gif", _=>"image/png" };
				sb.AppendLine ( $"      <img src='data:{mimeType};base64,{base64}' alt='Logo' style='max-height:3em;max-width:5em;object-fit:contain;display:block;margin-right:0.75em;vertical-align:middle;'/>" );
			}
			sb.AppendLine ( $"      <div style='font-size:2.2em;color:{headerColor};font-weight:700;white-space:nowrap;'>Developer Invoice</div>" );
			sb.AppendLine ( $"    </div>" );
			sb.AppendLine ( $"    <div style='font-size:1.1em;color:{amountDueColor};text-align:right;font-weight:600;flex-shrink:0;'>" );
			sb.AppendLine ( $"      <div><span style='font-weight:700;'>Date:</span> {DateTime.Now:yyyy-MM-dd}</div>" );
			sb.AppendLine ( $"      <div><span style='font-weight:700;'>Period:</span> {monthYear}</div>" );
			if ( !string.IsNullOrWhiteSpace ( config.Clockify.InvoiceNumber ) )
				sb.AppendLine ( $"      <div><span style='font-weight:700;'>Invoice #:</span> {config.Clockify.InvoiceNumber}</div>" );
			sb.AppendLine ( $"    </div>" );
			sb.AppendLine ( $"  </div>" );
			sb.AppendLine ( $"  <div style='display:flex;justify-content:space-between;gap:2em;flex-wrap:wrap;'>" );
			sb.AppendLine ( $"    <div style='width:48%;min-width:220px;'>" );
			sb.AppendLine ( $"      <div style='margin:0 0 0.3em 0;color:{headerColor};font-size:1.15em;font-weight:700;'>From:</div>" );
			sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>{config.Clockify.FromName}</p>" );
			sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>{config.Clockify.CompanyAddressLine1}<br>{config.Clockify.CompanyAddressLine2}<br>{config.Clockify.CompanyAddressLine3}</p>" );
			sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>Email: {config.Clockify.ContactEmail}<br>Phone: {config.Clockify.ContactPhone}</p>" );
			sb.AppendLine ( $"    </div>" );
			sb.AppendLine ( $"    <div style='width:48%;min-width:220px;'>" );
			sb.AppendLine ( $"      <div style='margin:0 0 0.3em 0;color:{headerColor};font-size:1.15em;font-weight:700;'>Bill To:</div>" );
			sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>{config.Clockify.ClientName}</p>" );
			sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>{config.Clockify.ClientAddress1}<br>{config.Clockify.ClientAddress2}<br>{config.Clockify.ClientAddress3}</p>" );
			sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>Email: {config.Clockify.ClientEmailAddress}<br>Phone: {config.Clockify.ClientNumber}</p>" );
			sb.AppendLine ( $"    </div>" );
			sb.AppendLine ( $"  </div>" );
			sb.AppendLine ( $"</div>" );

			sb.AppendLine ( $"<h2 style='color:{amountDueColor};font-size:1.4em;font-weight:700;margin-top:2em;margin-bottom:0.7em;text-align:left;background:#fff;padding:0.5em 1em;border-radius:6px;'>Work Summary</h2>" );
			sb.AppendLine ( $"<table style='width:100%;max-width:100%;border-collapse:collapse;margin-bottom:2em;box-shadow:0 4px 16px rgba(44,62,80,0.09);background:{sectionBg};border-radius:9px;overflow:hidden;'>" );
			sb.AppendLine ( $"<tr style='background:{tableHeaderBg};color:{headerColor};letter-spacing:0.03em;'>" );
			sb.AppendLine ( $"<th style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:left;font-weight:700;'>Project</th>" );
			sb.AppendLine ( $"<th style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Hours</th>" );
			sb.AppendLine ( $"<th style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Rate</th>" );
			sb.AppendLine ( $"<th style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Amount</th>" );
			sb.AppendLine ( $"</tr>" );

			double totalAmountTable = 0, totalHoursTable = 0;
			int rowIndex = 0;
			foreach ( dynamic group in projectGroups )
			{
				string rowBg = (rowIndex % 2 == 1) ? $"background:{altRowBg};" : "background:#fff;";
				double amount = group.Hours * config.Clockify.HourlyRate;
				sb.AppendLine ( $"<tr style='{rowBg}'>"
					+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:left;font-weight:500;color:{textColor};'>{group.Project}</td>"
					+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{textColor};'>{group.Hours:F2}</td>"
					+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{textColor};'>{config.Clockify.CurrencySymbol}{config.Clockify.HourlyRate:F2}</td>"
					+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{textColor};'>{config.Clockify.CurrencySymbol}{amount:F2}</td>"
					+ "</tr>" );
				totalAmountTable += amount;
				totalHoursTable += group.Hours;
				rowIndex++;
			}

			if ( config.ConstantLineItems?.Count > 0 )
			{
				sb.AppendLine ( $"<tr><td colspan='4' style='background:{headingBg};color:{headerColor};text-align:left;font-weight:700;border:1px solid {borderColor};padding:13px 10px;'>Other Charges</td></tr>" );
				foreach ( var item in config.ConstantLineItems )
				{
					string rowBg = (rowIndex % 2 == 1) ? $"background:{altRowBg};" : "background:#fff;";
					sb.AppendLine ( $"<tr style='{rowBg}'>"
					   + $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:left;font-weight:500;color:{textColor};'>{item.Description}</td>"
					   + $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;'></td>"
					   + $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;'></td>"
					   + $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{textColor};'>{config.Clockify.CurrencySymbol}{item.Amount:F2}</td>"
					   + "</tr>" );
					totalAmountTable += item.Amount;
					rowIndex++;
				}
			}

			sb.AppendLine ( $"<tr>"
				+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:left;font-weight:700;background:{tableHeaderBg};color:{headerColor};'>Total</td>"
				+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;background:{tableHeaderBg};color:{headerColor};'>{totalHoursTable:F2}</td>"
				+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;background:{tableHeaderBg};'></td>"
				+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;background:{tableHeaderBg};color:{headerColor};'>{config.Clockify.CurrencySymbol}{totalAmountTable:F2}</td>"
				+ "</tr>" );
			sb.AppendLine ( "</table>" );

			sb.AppendLine ( $"<div style='background:{headingBg};color:{amountDueColor};padding:1.3em 2.2em;border-radius:10px;display:inline-block;font-size:1.4em;margin-top:1.5em;margin-bottom:3em;font-weight:700;box-shadow:0 2px 8px rgba(44,62,80,0.06);'>Amount Due: {config.Clockify.CurrencySymbol}{totalAmountTable:F2}</div>" );

			sb.AppendLine ( "</div>" );
			sb.AppendLine ( "</body></html>" );

			return sb.ToString ( );
		}

		public async Task<(string Html, string PdfFilePath)> GenerateInvoiceAsync(
            DateTime start,
            DateTime end,
            InvoiceConfig config
        )
        {
            List<TimeEntryModel> entries = await _clockifyService.FetchTimeEntriesAsync(start, end, config);
            Serilog.Log.Information("[InvoiceService] Fetched {Count} time entries from Clockify.", entries.Count);

            ProjectNameCache projectNameCache = new(config.Clockify.ClockifyApiKey, config.Clockify.WorkspaceId);
            var seenEntryHashes = new HashSet<string>();
            var cleanEntries = new List<TimeEntryModel>();
            var projectEntryCount = new Dictionary<string, int>();
            var projectEntryHours = new Dictionary<string, double>();
            for (int i = 0; i < entries.Count; i++)
            {
                TimeEntryModel entry = entries[i];
                string entryHash = $"{entry.Start:o}|{entry.End:o}|{entry.ProjectId}|{entry.Description}|{entry.Hours}";
                if (seenEntryHashes.Contains(entryHash))
                {
                    Serilog.Log.Warning($"[InvoiceService] Duplicate time entry detected at index {i} (hash: {entryHash})");
                    continue;
                }
                seenEntryHashes.Add(entryHash);

                if (string.IsNullOrWhiteSpace(entry.ProjectName))
                {
                    if (!string.IsNullOrEmpty(entry.ProjectId))
                    {
                        entry.ProjectName = await projectNameCache.GetProjectNameAsync(entry.ProjectId);
                        if (string.IsNullOrWhiteSpace(entry.ProjectName))
                        {
                            entry.ProjectName = $"Unknown Project: {entry.ProjectId}";
                            Serilog.Log.Warning($"[InvoiceService] Could not resolve project name for projectId={entry.ProjectId} at index {i}");
                        }
                    }
                    else
                    {
                        entry.ProjectName = "No Project";
                        Serilog.Log.Warning($"[InvoiceService] Time entry at index {i} has no projectId and no project name.");
                    }
                }
                cleanEntries.Add(entry);
                if (!projectEntryCount.ContainsKey(entry.ProjectName!))
                {
                    projectEntryCount[entry.ProjectName!] = 0;
                    projectEntryHours[entry.ProjectName!] = 0;
                }
                projectEntryCount[entry.ProjectName!]++;
                projectEntryHours[entry.ProjectName!] += entry.Hours;
            }

            Serilog.Log.Information("[InvoiceService] Time entry summary by project:");
            double sumAllProjectHours = 0;
            foreach (var kvp in projectEntryCount)
            {
                Serilog.Log.Information($"[InvoiceService] Project: {kvp.Key}, Entries: {kvp.Value}, Hours: {projectEntryHours[kvp.Key]:F2}");
                sumAllProjectHours += projectEntryHours[kvp.Key];
            }
            Serilog.Log.Information($"[InvoiceService] SUM of all project hours: {sumAllProjectHours:F2}");

            double sumAllEntryHours = cleanEntries.Sum(e => e.Hours);
            Serilog.Log.Information($"[InvoiceService] SUM of all entry hours: {sumAllEntryHours:F2}");

            var projectGroups = cleanEntries
               .GroupBy(e => new { e.ProjectId, e.ProjectName })
               .Select(g => new
               {
                   ProjectId = g.Key.ProjectId,
                   Project = g.Key.ProjectName,
                   Hours = g.Sum(e => e.Hours)
               })
               .ToList();
            double sumGroupedProjectHours = projectGroups.Sum(g => g.Hours);
            Serilog.Log.Information($"[InvoiceService] SUM of grouped project hours (used in invoice): {sumGroupedProjectHours:F2}");

            double totalHours = projectGroups.Sum(g => g.Hours);
            double totalAmount = totalHours * config.Clockify.HourlyRate;
            string monthYear = start.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

            string html = BuildHtmlInvoice(
                projectGroups.Cast<object>().ToList(),
                config,
                monthYear,
                totalHours,
                totalAmount
            );

            string fromNameSafe = string.Join(" ", config.Clockify.FromName.Split(System.IO.Path.GetInvalidFileNameChars())).Trim();
            string clientNameSafe = string.Join(" ", config.Clockify.ClientName.Split(System.IO.Path.GetInvalidFileNameChars())).Trim();
            string fileName = $"{fromNameSafe} {clientNameSafe} {monthYear} Invoice";
            string pdfFilePath = System.IO.Path.Combine(
                config.Clockify.OutputPath,
                fileName + ".pdf"
            );

            // Ensure output directory exists and log the result
            try
            {
                if (!System.IO.Directory.Exists(config.Clockify.OutputPath))
                {
                    System.IO.Directory.CreateDirectory(config.Clockify.OutputPath);
                    Serilog.Log.Information($"[InvoiceService] Created output directory: {config.Clockify.OutputPath}");
                }
                else
                {
                    Serilog.Log.Information($"[InvoiceService] Output directory already exists: {config.Clockify.OutputPath}");
                }
            }
            catch (Exception dirEx)
            {
                Serilog.Log.Error(dirEx, $"[InvoiceService] Failed to create or access output directory: {config.Clockify.OutputPath}");
                throw;
            }

            try
            {
                await _pdfService.GeneratePdfAsync(html, pdfFilePath);
                Serilog.Log.Information($"[InvoiceService] PDF invoice saved: {pdfFilePath}");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"[InvoiceService] Failed to generate PDF invoice: {pdfFilePath}");
            }

            return (html, pdfFilePath);
        }
    }
}