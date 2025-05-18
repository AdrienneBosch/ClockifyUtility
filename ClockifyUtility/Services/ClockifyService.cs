using System.Net.Http;

using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
	public class ClockifyService : IClockifyService
	{
		public async Task<List<TimeEntryModel>> FetchTimeEntriesAsync ( DateTime start, DateTime end, ConfigModel config )
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
				Serilog.Log.Information("[Clockify] Requesting time entries for workspace {WorkspaceId} and user {UserId}.", config.WorkspaceId, config.UserId);
				HttpResponseMessage resp = await client.SendAsync(request);
				string respStr = await resp.Content.ReadAsStringAsync();
				if ( !resp.IsSuccessStatusCode )
				{
					Serilog.Log.Error("[ClockifyService] Error: {StatusCode}", resp.StatusCode);
					return entries;
				}
				Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse ( respStr );
				if ( obj [ "timeentries" ] is not Newtonsoft.Json.Linq.JArray arr )
				{
					Serilog.Log.Warning("[ClockifyService] No timeentries found in response.");
					return entries;
				}
				foreach ( Newtonsoft.Json.Linq.JToken item in arr )
				{
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
				Serilog.Log.Information("[Clockify] Parsed {Count} time entries.", entries.Count);
			}
			catch ( Exception ex )
			{
				Serilog.Log.Error(ex, "[ClockifyService] Error fetching or parsing time entries.");
			}
			return entries;
		}
	}
}