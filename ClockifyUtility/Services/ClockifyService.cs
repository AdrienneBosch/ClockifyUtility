
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
    public class ClockifyService : IClockifyService
    {
        public async Task<List<TimeEntryModel>> FetchTimeEntriesAsync(DateTime start, DateTime end, ConfigModel config, Action<string>? log = null)
        {
            var entries = new List<TimeEntryModel>();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", config.ClockifyApiKey);

            // Use the detailed report endpoint
            string url = $"https://reports.api.clockify.me/v1/workspaces/{config.WorkspaceId}/reports/detailed";
            var body = new {
                dateRangeStart = start.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                dateRangeEnd = end.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                exportType = "JSON",
                users = new { ids = new[] { config.UserId } },
                detailedFilter = new { page = 1, pageSize = 1000 }
            };
            var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Api-Key", config.ClockifyApiKey);
            try
            {
                log?.Invoke($"[Clockify] Requesting: {url} with body: {jsonBody}");
                var resp = await client.SendAsync(request);
                var respStr = await resp.Content.ReadAsStringAsync();
                log?.Invoke($"[Clockify] Response: {respStr.Substring(0, Math.Min(respStr.Length, 500))}{(respStr.Length > 500 ? "..." : "")}");
                if (!resp.IsSuccessStatusCode)
                {
                    log?.Invoke($"[ClockifyService] Error: {resp.StatusCode} {respStr}");
                    return entries;
                }
                var obj = Newtonsoft.Json.Linq.JObject.Parse(respStr);
                var arr = obj["timeentries"] as Newtonsoft.Json.Linq.JArray;
                if (arr == null)
                {
                    log?.Invoke("[ClockifyService] No timeentries found in response.");
                    return entries;
                }
                foreach (var item in arr)
                {
                    // Log the full entry for debugging
                    log?.Invoke($"[ClockifyService] ENTRY RAW: {item}");
                    var projectId = item["projectId"]?.ToString() ?? string.Empty;
                    string? project = null;
                    if (item["project"] != null && item["project"]["name"] != null)
                    {
                        project = item["project"]["name"]?.ToString();
                    }
                    var description = item["description"]?.ToString() ?? "";
                    var timeInterval = item["timeInterval"];
                    var startStr = timeInterval?["start"]?.ToString();
                    var endStr = timeInterval?["end"]?.ToString();
                    var durationToken = timeInterval?["duration"];

                    double hours = 0;
                    if (durationToken == null || durationToken.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                    {
                        log?.Invoke($"[ClockifyService] WARNING: duration missing for entry: {item}");
                    }
                    else if (durationToken.Type == Newtonsoft.Json.Linq.JTokenType.Integer || durationToken.Type == Newtonsoft.Json.Linq.JTokenType.Float)
                    {
                        double seconds = durationToken.ToObject<double>();
                        hours = seconds / 3600.0;
                        log?.Invoke($"[ClockifyService] duration (seconds) for entry: {seconds} (hours: {hours})");
                    }
                    else if (durationToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                    {
                        var durationStr = durationToken.ToObject<string>();
                        if (!string.IsNullOrEmpty(durationStr) && durationStr.StartsWith("PT"))
                        {
                            try
                            {
                                var ts = System.Xml.XmlConvert.ToTimeSpan(durationStr);
                                hours = ts.TotalHours;
                                log?.Invoke($"[ClockifyService] duration (ISO8601) for entry: {durationStr} (hours: {hours})");
                            }
                            catch (Exception ex)
                            {
                                log?.Invoke($"[ClockifyService] Failed to parse duration '{durationStr}': {ex.Message}");
                            }
                        }
                        else
                        {
                            log?.Invoke($"[ClockifyService] Unrecognized duration string format: {durationStr}");
                        }
                    }
                    else
                    {
                        log?.Invoke($"[ClockifyService] Unrecognized duration token type: {durationToken.Type}");
                    }

                    DateTime s, e;
                    var parsedStart = DateTime.TryParse(startStr, out s) ? s : start;
                    var parsedEnd = DateTime.TryParse(endStr, out e) ? e : end;

                    entries.Add(new TimeEntryModel
                    {
                        ProjectId = projectId,
                        ProjectName = project, // will be resolved in InvoiceService if null/empty
                        Description = description,
                        Start = parsedStart,
                        End = parsedEnd,
                        Hours = hours
                    });
                }
                log?.Invoke($"[Clockify] Parsed {entries.Count} time entries.");
            }
            catch (Exception ex)
            {
                log?.Invoke($"[ClockifyService] Error fetching or parsing time entries: {ex.Message}\nURL: {url}");
            }
            return entries;
        }
    }
}
