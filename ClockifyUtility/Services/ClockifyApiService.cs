using System.Net.Http;

using Newtonsoft.Json.Linq;

namespace ClockifyUtility.Services
{
	public class ClockifyApiService
	{
		private readonly string _apiKey;

		public ClockifyApiService ( string apiKey )
		{
			_apiKey = apiKey;
		}

		public async Task<string> GetUserIdAsync ( )
		{
			using HttpClient client = new();
			client.DefaultRequestHeaders.Add ( "X-Api-Key", _apiKey );
			string resp = await client.GetStringAsync("https://api.clockify.me/api/v1/user");
			JObject user = JObject.Parse(resp);
			return user [ "id" ]?.ToString ( ) ?? string.Empty;
		}

		public async Task<List<Models.WorkspaceInfo>> GetWorkspacesAsync ( )
		{
			using HttpClient client = new();
			client.DefaultRequestHeaders.Add ( "X-Api-Key", _apiKey );
			string resp = await client.GetStringAsync("https://api.clockify.me/api/v1/workspaces");
			JArray workspaces = JArray.Parse(resp);
			List<Models.WorkspaceInfo> result = [];
			foreach ( JToken ws in workspaces )
			{
				result.Add ( new Models.WorkspaceInfo
				{
					Name = ws [ "name" ]?.ToString ( ) ?? string.Empty,
					Id = ws [ "id" ]?.ToString ( ) ?? string.Empty
				} );
			}

			return result;
		}
	}
}