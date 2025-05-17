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

		public async Task<string> GenerateInvoiceAsync (
			DateTime start,
			DateTime end,
			ConfigModel config,
			Action<string>? log = null
		)
		{
			var entries = await _clockifyService.FetchTimeEntriesAsync(start, end, config, log);
			log?.Invoke ( $"[InvoiceService] Fetched {entries.Count} time entries from Clockify." );

			var projectNameCache = new ProjectNameCache(config.ClockifyApiKey, config.WorkspaceId);
			for ( int i = 0; i < entries.Count; i++ )
			{
				var entry = entries[i];
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

			var html = BuildHtmlInvoice(
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

		private string BuildHtmlInvoice (
			List<dynamic> projectGroups,
			ConfigModel config,
			string monthYear,
			double totalHours,
			double totalAmount
		)
		{
			var style = config.InvoiceStyle ?? new ClockifyUtility.Models.InvoiceStyleConfig();

			string headingBg = style.SoftHeadingBg ?? "#e3f0fa";
			string altRowBg = style.SoftAltRowBg ?? "#f6fbff";
			string border = style.TableBorder ?? "#BFC9D1";
			string headerBg = style.TableHeaderBg ?? "#D0E4FA";
			string primary = style.PrimaryColor ?? "#2C3E50";
			string secondary = style.SecondaryColor ?? "#2980B9";
			string accent = style.AccentColor ?? "#27AE60";
			string text = style.TextColor ?? "#1A1A1A";
			string background = style.BackgroundColor ?? "#F4F8FB";

			var sb = new StringBuilder();

			sb.AppendLine (
				$"<html><body style='background:{background};color:{text};font-family:Segoe UI,Arial,sans-serif;margin:0;padding:2em;min-height:100vh;'>"
			);

			sb.AppendLine (
				"<div style='max-width:730px;margin:0 auto;padding:0 2vw;'>"
			);

			sb.AppendLine (
				$"<div style='background:{headingBg};border-radius:14px;padding:1.8em 2em 1.3em 2em;margin-bottom:2em;box-shadow:0 4px 16px rgba(44,62,80,0.09);'>"
			);
			sb.AppendLine (
				"  <div style='display:flex;justify-content:space-between;align-items:flex-end;flex-wrap:wrap;margin-bottom:1em;'>"
			);
			sb.AppendLine (
				$"    <div style='font-size:2.2em;color:{primary};font-weight:700;'>Developer Invoice</div>"
			);
			sb.AppendLine (
				$"    <div style='font-size:1.1em;color:{secondary};text-align:right;font-weight:600;'>"
			);
			if ( !string.IsNullOrWhiteSpace ( config.InvoiceNumber ) )
				sb.AppendLine ( $"<div><span style='font-weight:700;'>Invoice #:</span> {config.InvoiceNumber}</div>" );
			sb.AppendLine ( $"<div><span style='font-weight:700;'>Date:</span> {DateTime.Now:yyyy-MM-dd}</div>" );
			sb.AppendLine ( $"<div><span style='font-weight:700;'>Period:</span> {monthYear}</div>" );
			sb.AppendLine ( "    </div>" );
			sb.AppendLine ( "  </div>" );
			sb.AppendLine (
				"  <div style='display:flex;justify-content:space-between;gap:2em;flex-wrap:wrap;'>"
			);
			sb.AppendLine ( "    <div style='width:48%;min-width:220px;'>" );
			sb.AppendLine (
				$"      <div style='margin:0 0 0.3em 0;color:{primary};font-size:1.15em;font-weight:700;'>From:</div>"
			);
			sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>{config.FromName}</p>" );
			sb.AppendLine (
				$"      <p style='margin:0.1em 0;font-size:1em;'>{config.CompanyAddressLine1}<br>{config.CompanyAddressLine2}<br>{config.CompanyAddressLine3}</p>"
			);
			sb.AppendLine (
				$"      <p style='margin:0.1em 0;font-size:1em;'>Email: {config.ContactEmail}<br>Phone: {config.ContactPhone}</p>"
			);
			sb.AppendLine ( "    </div>" );
			sb.AppendLine ( "    <div style='width:48%;min-width:220px;'>" );
			sb.AppendLine (
				$"      <div style='margin:0 0 0.3em 0;color:{primary};font-size:1.15em;font-weight:700;'>Bill To:</div>"
			);
			sb.AppendLine ( $"      <p style='margin:0.1em 0;font-size:1em;'>{config.ClientName}</p>" );
			sb.AppendLine (
				$"      <p style='margin:0.1em 0;font-size:1em;'>{config.ClientAddress1}<br>{config.ClientAddress2}<br>{config.ClientAddress3}</p>"
			);
			sb.AppendLine (
				$"      <p style='margin:0.1em 0;font-size:1em;'>Email: {config.ClientEmailAddress}<br>Phone: {config.ClientNumber}</p>"
			);
			sb.AppendLine ( "    </div>" );
			sb.AppendLine ( "  </div>" );
			sb.AppendLine ( "</div>" );

			sb.AppendLine (
				$"<h2 style='color:{secondary};font-size:1.4em;font-weight:700;margin-top:2em;margin-bottom:0.7em;text-align:left;'>Work Summary</h2>"
			);

			sb.AppendLine (
				$"<table style='width:100%;max-width:100%;border-collapse:collapse;margin-bottom:2em;box-shadow:0 4px 16px rgba(44,62,80,0.09);background:#fff;border-radius:9px;overflow:hidden;'>"
			);
			sb.AppendLine (
				$"<tr style='background:{headerBg};color:{primary};letter-spacing:0.03em;'>"
			);
			sb.AppendLine (
				$"<th style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:left;font-weight:700;'>Project</th>"
			);
			sb.AppendLine (
				$"<th style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Hours</th>"
			);
			sb.AppendLine (
				$"<th style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Rate</th>"
			);
			sb.AppendLine (
				$"<th style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;'>Amount</th>"
			);
			sb.AppendLine ( "</tr>" );

			double totalAmountTable = 0;
			double totalHoursTable = 0;
			int rowIndex = 0;

			foreach ( var group in projectGroups )
			{
				string rowBg = (rowIndex % 2 == 1)
					? $"background:{altRowBg};"
					: "background:#fff;";
				double amount = group.Hours * config.HourlyRate;
				sb.AppendLine (
					$"<tr style='{rowBg}'>"
					+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:left;font-weight:500;color:{text};'>{group.Project}</td>"
					+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{text};'>{group.Hours:F2}</td>"
					+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{text};'>{config.CurrencySymbol}{config.HourlyRate:F2}</td>"
					+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{text};'>{config.CurrencySymbol}{amount:F2}</td>"
					+ "</tr>"
				);
				totalAmountTable += amount;
				totalHoursTable += group.Hours;
				rowIndex++;
			}

			if ( config.ConstantLineItems != null && config.ConstantLineItems.Count > 0 )
			{
				sb.AppendLine (
					$"<tr><td colspan='4' style='background:{headingBg};color:{primary};text-align:left;font-weight:700;border:1px solid {border};padding:13px 10px;'>Other Charges</td></tr>"
				);
				foreach ( var item in config.ConstantLineItems )
				{
					string rowBg = (rowIndex % 2 == 1)
						? $"background:{altRowBg};"
						: "background:#fff;";
					sb.AppendLine (
						$"<tr style='{rowBg}'>"
						+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:left;font-weight:500;color:{text};'>{item.Description}</td>"
						+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;'></td>"
						+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;'></td>"
						+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:500;color:{text};'>{config.CurrencySymbol}{item.Amount:F2}</td>"
						+ "</tr>"
					);
					totalAmountTable += item.Amount;
					rowIndex++;
				}
			}

			sb.AppendLine (
				$"<tr>"
				+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:left;font-weight:700;background:{headerBg};color:{primary};'>Total</td>"
				+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;background:{headerBg};color:{primary};'>{totalHoursTable:F2}</td>"
				+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;background:{headerBg};'></td>"
				+ $"<td style='border:1px solid {border};padding:13px 10px;font-size:1em;text-align:right;font-weight:700;background:{headerBg};color:{primary};'>{config.CurrencySymbol}{totalAmountTable:F2}</td>"
				+ "</tr>"
			);
			sb.AppendLine ( "</table>" );

			sb.AppendLine (
				$"<div style='background:{headingBg};color:{primary};padding:1.3em 2.2em;border-radius:10px;display:inline-block;font-size:1.4em;margin-top:1.5em;margin-bottom:3em;font-weight:700;box-shadow:0 2px 8px rgba(44,62,80,0.06);'>Amount Due: {config.CurrencySymbol}{totalAmountTable:F2}</div>"
			);

			sb.AppendLine ( "</div>" );
			sb.AppendLine ( "</body></html>" );

			return sb.ToString ( );
		}
	}
}
