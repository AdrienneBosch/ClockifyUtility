using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ClockifyUtility.Services
{
    public class ClockifyApiService
    {
        private readonly string _apiKey;
        public ClockifyApiService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string> GetUserIdAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            var resp = await client.GetStringAsync("https://api.clockify.me/api/v1/user");
            var user = JObject.Parse(resp);
            return user["id"]?.ToString() ?? string.Empty;
        }

        public async Task<List<(string Name, string Id)>> GetWorkspacesAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            var resp = await client.GetStringAsync("https://api.clockify.me/api/v1/workspaces");
            var workspaces = JArray.Parse(resp);
            var result = new List<(string, string)>();
            foreach (var ws in workspaces)
                result.Add((ws["name"]?.ToString() ?? "", ws["id"]?.ToString() ?? ""));
            return result;
        }
    }
}
