using System.Net.Http;
using System.Xml;
using Newtonsoft.Json;
using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
	public class ClockifyService : IClockifyService
	{
		// Internal models for v1 API response
		private class TimeInterval
		{
			public string Duration { get; set; } = string.Empty;
			public DateTime Start { get; set; }
			public DateTime? End { get; set; }
		}
		private class TimeEntry
		{
			public string Id { get; set; } = string.Empty;
			public string? Description { get; set; }
			public string? ProjectId { get; set; }
			public TimeInterval TimeInterval { get; set; } = new ( );
		}

		public async Task<List<TimeEntryModel>> FetchTimeEntriesAsync ( DateTime start, DateTime end, InvoiceConfig config )
		{
			List<TimeEntryModel> entries = new();
			using HttpClient client = new();
			client.DefaultRequestHeaders.Add ( "X-Api-Key", config.Clockify.ClockifyApiKey );

			string dateRangeStart = start.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
			string dateRangeEnd = end.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
			int page = 1;
			int pageSize = 5000;
			while ( true )
			{
				string url = $"https://api.clockify.me/api/v1/workspaces/{config.Clockify.WorkspaceId}/user/{config.Clockify.UserId}/time-entries?start={dateRangeStart}&end={dateRangeEnd}&page={page}&page-size={pageSize}";
				try
				{
					Serilog.Log.Information ( $"[Clockify] Fetching page {page}..." );
					HttpResponseMessage resp = await client.GetAsync(url);
					string respStr = await resp.Content.ReadAsStringAsync();
					if ( !resp.IsSuccessStatusCode )
					{
						Serilog.Log.Error ( "[ClockifyService] Error: {StatusCode}", resp.StatusCode );
						break;
					}
					var timeEntries = JsonConvert.DeserializeObject<List<TimeEntry>>(respStr);
					if ( timeEntries == null || timeEntries.Count == 0 )
					{
						Serilog.Log.Information ( $"[Clockify] Page {page} returned 0 entries." );
						break;
					}
					Serilog.Log.Information ( $"[Clockify] Page {page} returned {timeEntries.Count} entries." );
					foreach ( var item in timeEntries )
					{
						double hours = 0;
						string durationStr = item.TimeInterval.Duration;
						if ( !string.IsNullOrEmpty ( durationStr ) )
						{
							if ( double.TryParse ( durationStr, out double seconds ) )
							{
								hours = seconds / 3600.0;
								Serilog.Log.Debug ( "[ClockifyService] duration (seconds) for entry: {Seconds} (hours: {Hours})", seconds, hours );
							}
							else if ( durationStr.StartsWith ( "PT" ) )
							{
								try
								{
									TimeSpan ts = XmlConvert.ToTimeSpan(durationStr);
									hours = ts.TotalHours;
									Serilog.Log.Debug ( "[ClockifyService] duration (ISO8601) for entry: {DurationStr} (hours: {Hours})", durationStr, hours );
								}
								catch ( Exception ex )
								{
									Serilog.Log.Warning ( "[ClockifyService] Failed to parse duration '{DurationStr}': {Error}", durationStr, ex.Message );
								}
							}
							else
							{
								Serilog.Log.Warning ( "[ClockifyService] Unrecognized duration string format: {DurationStr}", durationStr );
							}
						}
						else
						{
							Serilog.Log.Warning ( "[ClockifyService] WARNING: duration missing for entry." );
						}
						entries.Add ( new TimeEntryModel
						{
							ProjectId = item.ProjectId,
							ProjectName = null, // will be resolved in InvoiceService if null/empty
							Description = item.Description ?? string.Empty,
							Start = item.TimeInterval.Start,
							End = item.TimeInterval.End ?? item.TimeInterval.Start,
							Hours = hours
						} );
					}
					if ( timeEntries.Count < pageSize )
						break;
				}
				catch ( Exception ex )
				{
					Serilog.Log.Error ( ex, "[ClockifyService] Error fetching or parsing time entries." );
					break;
				}
				page++;
			}
			Serilog.Log.Information ( $"[Clockify] Pagination complete. Total entries fetched: {entries.Count}" );
			return entries;
		}
	}
}