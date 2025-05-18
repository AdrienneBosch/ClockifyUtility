using System.Net.Http;

using Newtonsoft.Json.Linq;

namespace ClockifyUtility.Services
{
	public class ProjectNameCache
	{
		private readonly string _apiKey;
		private readonly Dictionary<string, string> _cache = [];
		private readonly string _workspaceId;

		public ProjectNameCache ( string apiKey, string workspaceId )
		{
			_apiKey = apiKey;
			_workspaceId = workspaceId;
		}

	   public async Task<string> GetProjectNameAsync ( string projectId )
		{
		   if ( string.IsNullOrEmpty ( projectId ) )
		   {
			   return "No Project";
		   }

		   if ( _cache.TryGetValue ( projectId, out string? name ) )
		   {
			   return name;
		   }

		   string url = $"https://api.clockify.me/api/v1/workspaces/{_workspaceId}/projects/{projectId}";
		   using HttpClient client = new();
		   client.DefaultRequestHeaders.Add ( "X-Api-Key", _apiKey );
		   Serilog.Log.Debug("[ProjectNameCache] Querying project API for projectId=REDACTED");
		   HttpResponseMessage resp = await client.GetAsync(url);
		   if ( !resp.IsSuccessStatusCode )
		   {
			   Serilog.Log.Warning("[ProjectNameCache] Failed to fetch project name for projectId=REDACTED: {StatusCode}", resp.StatusCode);
			   _cache [ projectId ] = "Unknown Project";
			   return "Unknown Project";
		   }
			JObject obj = JObject.Parse(await resp.Content.ReadAsStringAsync());
			name = obj [ "name" ]?.ToString ( ) ?? "Unknown Project";
			_cache [ projectId ] = name;
			return name;
		}
	}
}