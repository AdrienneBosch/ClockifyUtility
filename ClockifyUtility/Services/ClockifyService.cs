
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

            // ISO 8601 format for Clockify API
            string startIso = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string endIso = end.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string url = $"https://api.clockify.me/api/v1/workspaces/{config.WorkspaceId}/user/{config.UserId}/time-entries?start={startIso}&end={endIso}";

            log?.Invoke($"[Clockify] Requesting: {url}");
            try
            {
                var resp = await client.GetStringAsync(url);
                log?.Invoke($"[Clockify] Response: {resp.Substring(0, Math.Min(resp.Length, 500))}{(resp.Length > 500 ? "..." : "")}");
                var arr = Newtonsoft.Json.Linq.JArray.Parse(resp);

                foreach (var item in arr)
                {
                    var project = item["project"]?["name"]?.ToString() ?? "No Project";
                    var description = item["description"]?.ToString() ?? "";
                    var timeInterval = item["timeInterval"];
                    var startStr = timeInterval?["start"]?.ToString();
                    var endStr = timeInterval?["end"]?.ToString();
                    var duration = timeInterval?["duration"]?.ToString();

                    double hours = 0;
                    if (!string.IsNullOrEmpty(duration) && duration.StartsWith("PT"))
                    {
                        try
                        {
                            var ts = System.Xml.XmlConvert.ToTimeSpan(duration);
                            hours = ts.TotalHours;
                        }
                        catch (Exception ex)
                        {
                            log?.Invoke($"[ClockifyService] Failed to parse duration '{duration}': {ex.Message}");
                        }
                    }

                    entries.Add(new TimeEntryModel
                    {
                        ProjectName = project,
                        Description = description,
                        Start = DateTime.TryParse(startStr, out var s) ? s : start,
                        End = DateTime.TryParse(endStr, out var e) ? e : end,
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
