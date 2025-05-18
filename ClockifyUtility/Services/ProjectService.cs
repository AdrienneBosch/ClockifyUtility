using System.Net.Http;

using ClockifyUtility.Models;

using Newtonsoft.Json.Linq;

namespace ClockifyUtility.Services
{
	public class ProjectService
	{
		private readonly string _apiKey;

		public ProjectService ( string apiKey )
		{
			_apiKey = apiKey;
		}

		public async Task<List<ProjectInfo>> GetProjectsAsync ( string workspaceId )
		{
			using HttpClient client = new();
			client.DefaultRequestHeaders.Add ( "X-Api-Key", _apiKey );
			string url = $"https://api.clockify.me/api/v1/workspaces/{workspaceId}/projects";
			string resp = await client.GetStringAsync(url);
			JArray arr = JArray.Parse(resp);
			List<ProjectInfo> result = [];
			foreach ( JToken item in arr )
			{
				result.Add ( new ProjectInfo
				{
					Id = item [ "id" ]?.ToString ( ) ?? string.Empty,
					Name = item [ "name" ]?.ToString ( ) ?? string.Empty
				} );
			}
			return result;
		}
	}
}