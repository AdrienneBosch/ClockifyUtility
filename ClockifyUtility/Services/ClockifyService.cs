using System.Net.Http;

using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
	public class ClockifyService : IClockifyService
	{
		public async Task<List<TimeEntryModel>> FetchTimeEntriesAsync ( DateTime start, DateTime end, InvoiceConfig config )
		{
			List<TimeEntryModel> entries = [];
			using HttpClient client = new();
			client.DefaultRequestHeaders.Add ( "X-Api-Key", config.Clockify.ClockifyApiKey );

			// Use the detailed report endpoint
			string url = $"https://reports.api.clockify.me/v1/workspaces/{config.Clockify.WorkspaceId}/reports/detailed";
			int page = 1;
			while (true)
			{
				Serilog.Log.Information($"[Clockify] Fetching page {page}...");
			   var body = new
			   {
				   dateRangeStart = start.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
				   dateRangeEnd = end.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
				   exportType = "JSON",
				   users = new { ids = new[] { config.Clockify.UserId } },
				   detailedFilter = new { page = page, pageSize = 50 }
			   };
				string debugBody = Newtonsoft.Json.JsonConvert.SerializeObject(body, Newtonsoft.Json.Formatting.Indented);
				Serilog.Log.Information($"[Clockify] Request body for page {page}: {debugBody}");
				string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);
				HttpRequestMessage request = new(HttpMethod.Post, url)
				{
					Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
				};
				request.Headers.Add ( "X-Api-Key", config.Clockify.ClockifyApiKey );
				try
				{
					Serilog.Log.Information($"[Clockify] Requesting time entries for workspace {{WorkspaceId}} and user {{UserId}}. Page: {{Page}}", config.Clockify.WorkspaceId, config.Clockify.UserId, page);
					HttpResponseMessage resp = await client.SendAsync(request);
					string respStr = await resp.Content.ReadAsStringAsync();
					if ( !resp.IsSuccessStatusCode )
					{
						Serilog.Log.Error("[ClockifyService] Error: {StatusCode}", resp.StatusCode);
						break;
					}
					Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse ( respStr );
					Serilog.Log.Information($"[Clockify] Raw response for page {page}: {respStr.Substring(0, Math.Min(respStr.Length, 1000))}");
					if ( obj [ "timeentries" ] is not Newtonsoft.Json.Linq.JArray arr )
					{
						Serilog.Log.Warning("[ClockifyService] No timeentries found in response.");
						break;
					}
				Serilog.Log.Information($"[Clockify] Page {page} returned {arr.Count} entries.");
				if (arr.Count == 0)
				{
					break;
				}
					foreach ( Newtonsoft.Json.Linq.JToken item in arr )
					{
						string projectId = item["projectId"]?.ToString() ?? string.Empty;
						string? project = null;
						var projectToken = item["project"];
						if (projectToken != null && projectToken["name"] != null)
						{
							project = projectToken["name"]?.ToString();
						}
						string description = item["description"]?.ToString() ?? "";
						Newtonsoft.Json.Linq.JToken? timeInterval = item [ "timeInterval" ];
						string? startStr = timeInterval? [ "start" ]?.ToString ( );
						string? endStr = timeInterval? [ "end" ]?.ToString ( );
						Newtonsoft.Json.Linq.JToken? durationToken = timeInterval? [ "duration" ];

					double hours = 0;
					if ( durationToken == null || durationToken.Type == Newtonsoft.Json.Linq.JTokenType.Null )
					{
						Serilog.Log.Warning("[ClockifyService] WARNING: duration missing for entry.");
					}
					else if ( durationToken.Type is Newtonsoft.Json.Linq.JTokenType.Integer or Newtonsoft.Json.Linq.JTokenType.Float )
					{
						double seconds = durationToken.ToObject<double>();
						hours = seconds / 3600.0;
						Serilog.Log.Debug("[ClockifyService] duration (seconds) for entry: {Seconds} (hours: {Hours})", seconds, hours);
					}
					else if ( durationToken.Type == Newtonsoft.Json.Linq.JTokenType.String )
					{
						string? durationStr = durationToken.ToObject<string> ( );
						if ( !string.IsNullOrEmpty ( durationStr ) && durationStr.StartsWith ( "PT" ) )
						{
							try
							{
								TimeSpan ts = System.Xml.XmlConvert.ToTimeSpan(durationStr);
								hours = ts.TotalHours;
								Serilog.Log.Debug("[ClockifyService] duration (ISO8601) for entry: {DurationStr} (hours: {Hours})", durationStr, hours);
							}
							catch ( Exception ex )
							{
								Serilog.Log.Warning("[ClockifyService] Failed to parse duration '{DurationStr}': {Error}", durationStr, ex.Message);
							}
						}
						else
						{
							Serilog.Log.Warning("[ClockifyService] Unrecognized duration string format: {DurationStr}", durationStr);
						}
					}
					else
					{
						Serilog.Log.Warning("[ClockifyService] Unrecognized duration token type: {TokenType}", durationToken.Type);
					}

					DateTime parsedStart = !string.IsNullOrEmpty(startStr)
						? DateTime.Parse(startStr, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal)
						: start;
					DateTime parsedEnd = !string.IsNullOrEmpty(endStr)
						? DateTime.Parse(endStr, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal)
						: end;

					entries.Add ( new TimeEntryModel
					{
						ProjectId = projectId,
						ProjectName = project, // will be resolved in InvoiceService if null/empty
						Description = description,
						Start = parsedStart,
						End = parsedEnd,
						Hours = hours
					} );
					}
				}
				catch ( Exception ex )
				{
					Serilog.Log.Error(ex, "[ClockifyService] Error fetching or parsing time entries.");
					break;
				}
				page++;
			}
			Serilog.Log.Information($"[Clockify] Pagination complete. Total entries fetched: {entries.Count}");
			return entries;
		}
	}
}