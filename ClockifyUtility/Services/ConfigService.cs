using ClockifyUtility.Models;

using Microsoft.Extensions.Configuration;

namespace ClockifyUtility.Services
{
	public class ConfigService : IConfigService
	{
		public ConfigModel LoadConfig ( )
		{
			IConfigurationBuilder builder = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

			IConfigurationRoot configuration = builder.Build();
			ConfigModel configModel = new();
			configuration.GetSection ( "Clockify" ).Bind ( configModel );

			// If UserId or WorkspaceId is missing or placeholder, throw a custom exception
			return string.IsNullOrWhiteSpace ( configModel.UserId ) ||
				configModel.UserId == "your-user-id" ||
				string.IsNullOrWhiteSpace ( configModel.WorkspaceId ) ||
				configModel.WorkspaceId == "your-worksapace-id"
				? throw new MissingClockifyIdException ( configModel.ClockifyApiKey )
				: configModel;
		}
	}
}