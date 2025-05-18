using System.Globalization;
using System.Text;

using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
	public class InvoiceService : IInvoiceService
	{
		private readonly IClockifyService _clockifyService;
		private readonly IFileService _fileService;
		private readonly ProjectService _projectService;

		public InvoiceService (
			IClockifyService clockifyService,
			IFileService fileService,
			ProjectService projectService
		)
		{
			_clockifyService = clockifyService;
			_fileService = fileService;
			_projectService = projectService;
		}

		private string BuildHtmlInvoice (
			List<dynamic> projectGroups,
			ConfigModel config,
			string monthYear,
			double totalHours,
			double totalAmount
		)
		{

			InvoiceStyleConfig style = config.InvoiceStyle ?? new ClockifyUtility.Models.InvoiceStyleConfig();

			// Font settings
			string fontFamily = config.InvoiceFontFamily ?? "Segoe UI, Arial, sans-serif";
			string fontWeight = config.InvoiceFontWeight ?? "500";

			// Map mode: override primary/secondary colors to red if enabled
			string headerColor = (config.InvoiceMapMode ? "#FF0000" : style.PrimaryColor ?? "#2C3E50");
			string amountDueColor = (config.InvoiceMapMode ? "#FF0000" : style.SecondaryColor ?? "#2980B9");

			// More descriptive color names
			string headingBg = style.SoftHeadingBg ?? "#e3f0fa";
			string altRowBg = style.SoftAltRowBg ?? "#f6fbff";
			string borderColor = style.TableBorder ?? "#BFC9D1";
			string tableHeaderBg = style.TableHeaderBg ?? "#D0E4FA";
			string accentColor = style.AccentColor ?? "#27AE60";
			string textColor = style.TextColor ?? "#181818";
			string backgroundColor = style.BackgroundColor ?? "#F4F8FB";
			string sectionBg = style.SectionBg ?? "#ffffff";
			string sectionText = style.SectionText ?? "#181818";


			StringBuilder sb = new();

			_ = sb.AppendLine (
				$"<html><body style='background:{backgroundColor};color:{textColor};font-family:{fontFamily};font-weight:{fontWeight};margin:0;padding:2em;min-height:100vh;'>"
			);

			_ = sb.AppendLine (
				$"<div style='max-width:730px;margin:0 auto;padding:0 2vw;background:{sectionBg};color:{sectionText};border-radius:10px;box-shadow:0 2px 8px rgba(44,62,80,0.04);'>"
			);

			_ = sb.AppendLine (
				$"<div style='background:{headingBg};border-radius:14px;padding:1.8em 2em 1.3em 2em;margin-bottom:2em;box-shadow:0 4px 16px rgba(44,62,80,0.09);color:{textColor};'>"
			);
			_ = sb.AppendLine (
				"  <div style='display:flex;justify-content:space-between;align-items:flex-end;flex-wrap:wrap;margin-bottom:1em;'>"
			);
			_ = sb.AppendLine (
				$"    <div style='font-size:2.2em;color:{headerColor};font-weight:700;'>Developer Invoice</div>"
			);
			_ = sb.AppendLine (
				$"    <div style='font-size:1.1em;color:{amountDueColor};text-align:right;font-weight:600;'>"
			);
			if ( !string.IsNullOrWhiteSpace ( config.InvoiceNumber ) )
			{
				_ = sb.AppendLine ( $"<div><span style='font-weight:700;'>Invoice #:</span> {config.InvoiceNumber}</div>" );
			}

			_ = sb.AppendLine ( $"<div><span style='font-weight:700;'>Date:</span> {DateTime.Now:yyyy-MM-dd}</div>" );
			_ = sb.AppendLine ( $"<div><span style='font-weight:700;'>Period:</span> {monthYear}</div>" );
			_ = sb.AppendLine ( "    </div>" );
			_ = sb.AppendLine ( "  </div>" );
			_ = sb.AppendLine (
				"  <div style='display:flex;justify-content:space-between;gap:2em;flex-wrap:wrap;'>"
			);
			_ = sb.AppendLine ( "    <div style='width:48%;min-width:220px;'>" );
			_ = sb.AppendLine (
				$"      <div style='margin:0 0 0.3em 0;color:{headerColor};font-size:1.15em;font-weight:700;'>From:</div>"
			);
			_ = sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>{config.FromName}</p>" );
			_ = sb.AppendLine (
				$"      <p style='margin:0.1em 0;font-size:1em;'>{config.CompanyAddressLine1}<br>{config.CompanyAddressLine2}<br>{config.CompanyAddressLine3}</p>"
			);
			_ = sb.AppendLine (
				$"      <p style='margin:0.1em 0;font-size:1em;'>Email: {config.ContactEmail}<br>Phone: {config.ContactPhone}</p>"
			);
			_ = sb.AppendLine ( "    </div>" );
			_ = sb.AppendLine ( "    <div style='width:48%;min-width:220px;'>" );
			_ = sb.AppendLine (
				$"      <div style='margin:0 0 0.3em 0;color:{headerColor};font-size:1.15em;font-weight:700;'>Bill To:</div>"
			);
			_ = sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>{config.ClientName}</p>" );
			_ = sb.AppendLine (
				$"      <p style='margin:0.1em 0;font-size:1em;'>{config.ClientAddress1}<br>{config.ClientAddress2}<br>{config.ClientAddress3}</p>"
			);
			_ = sb.AppendLine (
				$"      <p style='margin:0.1em 0;font-size:1em;'>Email: {config.ClientEmailAddress}<br>Phone: {config.ClientNumber}</p>"
			);
			_ = sb.AppendLine ( "    </div>" );
			_ = sb.AppendLine ( "  </div>" );
			_ = sb.AppendLine ( "</div>" );

			_ = sb.AppendLine (
				$"<h2 style='color:{amountDueColor};font-size:1.4em;font-weight:700;margin-top:2em;margin-bottom:0.7em;text-align:left;background:#fff;padding:0.5em 1em;border-radius:6px;'>Work Summary</h2>"
			);

			_ = sb.AppendLine (
				$"<table style='width:100%;max-width:100%;border-collapse:collapse;margin-bottom:2em;box-shadow:0 4px 16px rgba(44,62,80,0.09);background:{sectionBg};border-radius:9px;overflow:hidden;'>"
			);
			_ = sb.AppendLine (
				$"<tr style='background:{tableHeaderBg};color:{headerColor};letter-spacing:0.03em;'>"
			);
			_ = sb.AppendLine (
				$"<th style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:left;font-weight:700;'>Project</th>"
			);
			_ = sb.AppendLine (
				$"<th style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Hours</th>"
			);
			_ = sb.AppendLine (
				$"<th style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Rate</th>"
			);
			_ = sb.AppendLine (
				$"<th style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Amount</th>"
			);
			_ = sb.AppendLine ( "</tr>" );

			double totalAmountTable = 0;
			double totalHoursTable = 0;
			int rowIndex = 0;

			foreach ( dynamic group in projectGroups )
			{
				string rowBg = (rowIndex % 2 == 1)
					? $"background:{altRowBg};"
					: "background:#fff;";
				double amount = group.Hours * config.HourlyRate;
				_ = sb.AppendLine (
					$"<tr style='{rowBg}'>"
					+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:left;font-weight:500;color:{textColor};'>{group.Project}</td>"
					+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{textColor};'>{group.Hours:F2}</td>"
					+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{textColor};'>{config.CurrencySymbol}{config.HourlyRate:F2}</td>"
					+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{textColor};'>{config.CurrencySymbol}{amount:F2}</td>"
					+ "</tr>"
				);
				totalAmountTable += amount;
				totalHoursTable += group.Hours;
				rowIndex++;
			}

			if ( config.ConstantLineItems != null && config.ConstantLineItems.Count > 0 )
			{
				_ = sb.AppendLine (
					$"<tr><td colspan='4' style='background:{headingBg};color:{headerColor};text-align:left;font-weight:700;border:1px solid {borderColor};padding:13px 10px;'>Other Charges</td></tr>"
				);
				foreach ( ConstantLineItem item in config.ConstantLineItems )
				{
					string rowBg = (rowIndex % 2 == 1)
						? $"background:{altRowBg};"
						: "background:#fff;";
					_ = sb.AppendLine (
						$"<tr style='{rowBg}'>"
						+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:left;font-weight:500;color:{textColor};'>{item.Description}</td>"
						+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;'></td>"
						+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;'></td>"
						+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{textColor};'>{config.CurrencySymbol}{item.Amount:F2}</td>"
						+ "</tr>"
					);
					totalAmountTable += item.Amount;
					rowIndex++;
				}
			}

			_ = sb.AppendLine (
				$"<tr>"
				+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:left;font-weight:700;background:{tableHeaderBg};color:{headerColor};'>Total</td>"
				+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;background:{tableHeaderBg};color:{headerColor};'>{totalHoursTable:F2}</td>"
				+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;background:{tableHeaderBg};'></td>"
				+ $"<td style='border:1px solid {borderColor};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;background:{tableHeaderBg};color:{headerColor};'>{config.CurrencySymbol}{totalAmountTable:F2}</td>"
				+ "</tr>"
			);
			_ = sb.AppendLine ( "</table>" );

			_ = sb.AppendLine (
				$"<div style='background:{headingBg};color:{amountDueColor};padding:1.3em 2.2em;border-radius:10px;display:inline-block;font-size:1.4em;margin-top:1.5em;margin-bottom:3em;font-weight:700;box-shadow:0 2px 8px rgba(44,62,80,0.06);'>Amount Due: {config.CurrencySymbol}{totalAmountTable:F2}</div>"
			);

			_ = sb.AppendLine ( "</div>" );
			_ = sb.AppendLine ( "</body></html>" );

			return sb.ToString ( );
		}

		public async Task<string> GenerateInvoiceAsync (
					DateTime start,
			DateTime end,
			ConfigModel config,
			Action<string>? log = null
		)
		{
			List<TimeEntryModel> entries = await _clockifyService.FetchTimeEntriesAsync ( start, end, config, log );
			log?.Invoke ( $"[InvoiceService] Fetched {entries.Count} time entries from Clockify." );

			ProjectNameCache projectNameCache = new(config.ClockifyApiKey, config.WorkspaceId);
			for ( int i = 0; i < entries.Count; i++ )
			{
				TimeEntryModel entry = entries[i];
				log?.Invoke ( $"[InvoiceService] TimeEntry {i}: projectId={entry.ProjectId}" );
				if ( string.IsNullOrEmpty ( entry.ProjectName ) )
				{
					entry.ProjectName = await projectNameCache.GetProjectNameAsync ( entry.ProjectId, log );
				}
				if ( string.IsNullOrEmpty ( entry.ProjectName ) )
				{
					entry.ProjectName = "No Project";
				}
			}

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

			string html = BuildHtmlInvoice(
				projectGroups.Cast<object>().ToList(),
				config,
				monthYear,
				totalHours,
				totalAmount
			);

			string filePath = System.IO.Path.Combine(
				config.OutputPath,
				$"Invoice_{monthYear.Replace(" ", "_")}.html"
			);
			await _fileService.SaveHtmlAsync ( html, filePath );
			return filePath;
		}
	}
}