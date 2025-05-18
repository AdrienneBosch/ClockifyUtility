using System.Net.Http;

using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
	public class ClockifyService : IClockifyService
	{
		public async Task<List<TimeEntryModel>> FetchTimeEntriesAsync ( DateTime start, DateTime end, ConfigModel config, Action<string>? log = null )
		{
			List<TimeEntryModel> entries = [];
			using HttpClient client = new();
			client.DefaultRequestHeaders.Add ( "X-Api-Key", config.ClockifyApiKey );

			// Use the detailed report endpoint
			string url = $"https://reports.api.clockify.me/v1/workspaces/{config.WorkspaceId}/reports/detailed";
			var body = new
			{
				dateRangeStart = start.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
				dateRangeEnd = end.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
				exportType = "JSON",
				users = new { ids = new[] { config.UserId } },
				detailedFilter = new { page = 1, pageSize = 1000 }
			};
			string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);
			HttpRequestMessage request = new(HttpMethod.Post, url)
			{
				Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
			};
			request.Headers.Add ( "X-Api-Key", config.ClockifyApiKey );
			try
			{
				log?.Invoke ( $"[Clockify] Requesting: {url} with body: {jsonBody}" );
				HttpResponseMessage resp = await client.SendAsync(request);
				string respStr = await resp.Content.ReadAsStringAsync();
				log?.Invoke ( $"[Clockify] Response: {respStr [ ..Math.Min ( respStr.Length, 500 ) ]}{( respStr.Length > 500 ? "..." : "" )}" );
				if ( !resp.IsSuccessStatusCode )
				{
					log?.Invoke ( $"[ClockifyService] Error: {resp.StatusCode} {respStr}" );
					return entries;
				}
				Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse ( respStr );
				if ( obj [ "timeentries" ] is not Newtonsoft.Json.Linq.JArray arr )
				{
					log?.Invoke ( "[ClockifyService] No timeentries found in response." );
					return entries;
				}
				foreach ( Newtonsoft.Json.Linq.JToken item in arr )
				{
					// Log the full entry for debugging
					log?.Invoke ( $"[ClockifyService] ENTRY RAW: {item}" );
					string projectId = item["projectId"]?.ToString() ?? string.Empty;
					string? project = null;
					if ( item [ "project" ] != null && item [ "project" ] [ "name" ] != null )
					{
						project = item [ "project" ] [ "name" ]?.ToString ( );
					}
					string description = item["description"]?.ToString() ?? "";
					Newtonsoft.Json.Linq.JToken? timeInterval = item [ "timeInterval" ];
					string? startStr = timeInterval? [ "start" ]?.ToString ( );
					string? endStr = timeInterval? [ "end" ]?.ToString ( );
					Newtonsoft.Json.Linq.JToken? durationToken = timeInterval? [ "duration" ];

					double hours = 0;
					if ( durationToken == null || durationToken.Type == Newtonsoft.Json.Linq.JTokenType.Null )
					{
						log?.Invoke ( $"[ClockifyService] WARNING: duration missing for entry: {item}" );
					}
					else if ( durationToken.Type is Newtonsoft.Json.Linq.JTokenType.Integer or Newtonsoft.Json.Linq.JTokenType.Float )
					{
						double seconds = durationToken.ToObject<double>();
						hours = seconds / 3600.0;
						log?.Invoke ( $"[ClockifyService] duration (seconds) for entry: {seconds} (hours: {hours})" );
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
								log?.Invoke ( $"[ClockifyService] duration (ISO8601) for entry: {durationStr} (hours: {hours})" );
							}
							catch ( Exception ex )
							{
								log?.Invoke ( $"[ClockifyService] Failed to parse duration '{durationStr}': {ex.Message}" );
							}
						}
						else
						{
							log?.Invoke ( $"[ClockifyService] Unrecognized duration string format: {durationStr}" );
						}
					}
					else
					{
						log?.Invoke ( $"[ClockifyService] Unrecognized duration token type: {durationToken.Type}" );
					}

					DateTime parsedStart = DateTime.TryParse(startStr, out DateTime s) ? s : start;
					DateTime parsedEnd = DateTime.TryParse(endStr, out DateTime e) ? e : end;

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
				log?.Invoke ( $"[Clockify] Parsed {entries.Count} time entries." );
			}
			catch ( Exception ex )
			{
				log?.Invoke ( $"[ClockifyService] Error fetching or parsing time entries: {ex.Message}\nURL: {url}" );
			}
			return entries;
		}
	}
}