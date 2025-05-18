using Serilog;
using System.Windows;

using ClockifyUtility.Services;
using ClockifyUtility.ViewModels;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
using Microsoft.Extensions.DependencyInjection;

namespace ClockifyUtility;
public partial class App : Application
{
	private static bool _serilogInitialized = false;
	public static ServiceProvider? ServiceProvider { get; private set; }

	protected override void OnStartup ( StartupEventArgs e )
	{
		// Step 4: Load and validate all invoice configs at startup
	   string exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
	   string appSettingsPath = System.IO.Path.Combine(exeDir, "appsettings.json");
	   string? invoiceConfigDir = null;
	   if (System.IO.File.Exists(appSettingsPath))
	   {
		   var json = System.IO.File.ReadAllText(appSettingsPath);
		   var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ClockifyUtility.ViewModels.AppSettings>(json);
		   invoiceConfigDir = settings?.InvoiceConfigDirectory;
	   }
	   string configDir = invoiceConfigDir ?? System.IO.Path.Combine(exeDir, "invoice-generator");
	   var results = ClockifyUtility.Services.InvoiceConfigLoader.LoadAllConfigs(configDir);
		var errorMsgs = new System.Text.StringBuilder();
		foreach (var result in results)
		{
			if (result.Errors != null && result.Errors.Count > 0)
			{
				errorMsgs.AppendLine($"File: {System.IO.Path.GetFileName(result.FilePath)}");
				foreach (var err in result.Errors)
					errorMsgs.AppendLine($"  - {err}");
				errorMsgs.AppendLine();
			}
		}
		if (errorMsgs.Length > 0)
		{
			System.Windows.MessageBox.Show(
				$"Some invoice configuration files are invalid:\n\n{errorMsgs}",
				"Invoice Config Validation Error",
				MessageBoxButton.OK,
				MessageBoxImage.Error
			);
			System.Windows.Application.Current.Shutdown();
			return;
		}
		if ( !_serilogInitialized )
		{
			Log.Logger = new LoggerConfiguration ( )
				.MinimumLevel.Information ( )
				.WriteTo.File (
					path: "logs/clockify-utility-.log",
					rollingInterval: RollingInterval.Day,
					retainedFileCountLimit: 7,
					outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
				)
				.CreateLogger ( );
			_serilogInitialized = true;
		}

		ServiceCollection services = new();
		_ = services.AddSingleton<IClockifyService, ClockifyService> ( );
		_ = services.AddSingleton<IFileService, FileService> ( );
		_ = services.AddSingleton<IConfigService, ConfigService> ( );
		_ = services.AddSingleton<ProjectService> ( sp =>
		{
			var config = sp.GetRequiredService<IConfigService>().LoadConfig();
			var apiKey = config.ClockifyApiKey;
			if (string.IsNullOrWhiteSpace(apiKey))
				throw new InvalidOperationException("ClockifyApiKey is missing in appsettings.json (Clockify section)");
			return new ProjectService(apiKey);
		});
		_ = services.AddSingleton<IInvoiceService> ( sp =>
			new InvoiceService (
				sp.GetRequiredService<IClockifyService> ( ),
				sp.GetRequiredService<IFileService> ( ),
				sp.GetRequiredService<ProjectService> ( )
			)
		);
		_ = services.AddSingleton<MainViewModel> ( );
		ServiceProvider = services.BuildServiceProvider ( );
		base.OnStartup ( e );
	}

	protected override void OnExit(ExitEventArgs e)
	{
		Log.CloseAndFlush();
		base.OnExit(e);
	}
}