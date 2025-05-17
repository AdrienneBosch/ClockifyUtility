using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ClockifyUtility.Models;
using Newtonsoft.Json.Linq;

namespace ClockifyUtility.Services
{
    public class ProjectService
    {
        private readonly string _apiKey;
        public ProjectService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<List<ProjectInfo>> GetProjectsAsync(string workspaceId)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            var url = $"https://api.clockify.me/api/v1/workspaces/{workspaceId}/projects";
            var resp = await client.GetStringAsync(url);
            var arr = JArray.Parse(resp);
            var result = new List<ProjectInfo>();
            foreach (var item in arr)
            {
                result.Add(new ProjectInfo
                {
                    Id = item["id"]?.ToString() ?? string.Empty,
                    Name = item["name"]?.ToString() ?? string.Empty
                });
            }
            return result;
        }
    }
}
