using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
    public class ProjectNameCache
    {
        private readonly string _apiKey;
        private readonly string _workspaceId;
        private readonly Dictionary<string, string> _cache = new();
        public ProjectNameCache(string apiKey, string workspaceId)
        {
            _apiKey = apiKey;
            _workspaceId = workspaceId;
        }

        public async Task<string> GetProjectNameAsync(string projectId, Action<string>? log = null)
        {
            if (string.IsNullOrEmpty(projectId)) return "No Project";
            if (_cache.TryGetValue(projectId, out var name))
                return name;

            var url = $"https://api.clockify.me/api/v1/workspaces/{_workspaceId}/projects/{projectId}";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            log?.Invoke($"[ProjectNameCache] Querying project API: {url}");
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                log?.Invoke($"[ProjectNameCache] Failed to fetch project name for {projectId}: {resp.StatusCode}");
                _cache[projectId] = "Unknown Project";
                return "Unknown Project";
            }
            var obj = JObject.Parse(await resp.Content.ReadAsStringAsync());
            name = obj["name"]?.ToString() ?? "Unknown Project";
            _cache[projectId] = name;
            return name;
        }
    }
}
