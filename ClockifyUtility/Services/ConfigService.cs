using ClockifyUtility.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace ClockifyUtility.Services
{
    public class ConfigService : IConfigService
    {
        public ConfigModel LoadConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            var configModel = new ConfigModel();
            configuration.GetSection("Clockify").Bind(configModel);

            // If UserId or WorkspaceId is missing, throw a custom exception
            if (string.IsNullOrWhiteSpace(configModel.UserId) || string.IsNullOrWhiteSpace(configModel.WorkspaceId))
            {
                throw new MissingClockifyIdException(configModel.ClockifyApiKey);
            }
            return configModel;
        }
    }
}
