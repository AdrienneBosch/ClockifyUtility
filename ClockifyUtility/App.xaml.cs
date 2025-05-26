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
	   if (string.IsNullOrWhiteSpace(invoiceConfigDir) || !System.IO.Directory.Exists(invoiceConfigDir))
	   {
		   System.Windows.MessageBox.Show(
			   $"Invoice config directory is not set or does not exist: {invoiceConfigDir}",
			   "Invoice Config Directory Error",
			   MessageBoxButton.OK,
			   MessageBoxImage.Error
		   );
		   System.Windows.Application.Current.Shutdown();
		   return;
	   }
	   var results = ClockifyUtility.Services.InvoiceConfigLoader.LoadAllConfigs(invoiceConfigDir);
		var errorMsgs = new System.Text.StringBuilder();
		var criticalErrorMsgs = new System.Text.StringBuilder();
		foreach (var result in results)
		{
			if (result.Errors != null && result.Errors.Count > 0)
			{
				// Only treat as critical if errors are not just missing UserId/WorkspaceId
				var nonIdErrors = result.Errors.FindAll(err =>
					!(err.Contains("UserId is required.") || err.Contains("WorkspaceId is required.")));
				if (nonIdErrors.Count > 0)
				{
					criticalErrorMsgs.AppendLine($"File: {System.IO.Path.GetFileName(result.FilePath)}");
					foreach (var err in result.Errors)
						criticalErrorMsgs.AppendLine($"  - {err}");
					criticalErrorMsgs.AppendLine();
				}
				// Always show all errors in the main errorMsgs for possible later use
				errorMsgs.AppendLine($"File: {System.IO.Path.GetFileName(result.FilePath)}");
				foreach (var err in result.Errors)
					errorMsgs.AppendLine($"  - {err}");
				errorMsgs.AppendLine();
			}
		}
		if (criticalErrorMsgs.Length > 0)
		{
			System.Windows.MessageBox.Show(
				$"Some invoice configuration files are invalid:\n\n{criticalErrorMsgs}",
				"Invoice Config Validation Error",
				MessageBoxButton.OK,
				MessageBoxImage.Error
			);
			System.Windows.Application.Current.Shutdown();
			return;
		}
	   if (!_serilogInitialized)
	   {
		   // Ensure logs directory exists
		   var logDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "logs");
		   if (!System.IO.Directory.Exists(logDir))
		   {
			   System.IO.Directory.CreateDirectory(logDir);
		   }
		   Log.Logger = new LoggerConfiguration()
			   .MinimumLevel.Information()
			   .WriteTo.File(
				   path: "logs/clockify-utility-.log",
				   rollingInterval: RollingInterval.Day,
				   retainedFileCountLimit: 7,
				   outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
			   )
			   .CreateLogger();
		   _serilogInitialized = true;
	   }

		ServiceCollection services = new();
		_ = services.AddSingleton<IClockifyService, ClockifyService> ( );
		_ = services.AddSingleton<IFileService, FileService> ( );
		_ = services.AddSingleton<IPdfService, PdfService> ( );
		_ = services.AddSingleton<IConfigService, ConfigService> ( );
		// Defer ProjectService config loading to avoid startup crash if config is invalid
		_ = services.AddSingleton<ProjectService>(sp =>
		{
			var configService = sp.GetRequiredService<IConfigService>();
			string? apiKey = null;
			try
			{
				var config = configService.LoadConfig();
				apiKey = config.ClockifyApiKey;
			}
			catch (Exception ex)
			{
				// Log and allow ProjectService to be created with a dummy key; will fail later if used
				Serilog.Log.Warning(ex, "ProjectService: Could not load config at startup. Will require valid config at use time.");
				apiKey = string.Empty;
			}
			return new ProjectService(apiKey ?? string.Empty);
		});
		_ = services.AddSingleton<IInvoiceService> ( sp =>
			new InvoiceService (
				sp.GetRequiredService<IClockifyService> ( ),
				sp.GetRequiredService<IFileService> ( ),
				sp.GetRequiredService<ProjectService> ( ),
				sp.GetRequiredService<IPdfService> ( )
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