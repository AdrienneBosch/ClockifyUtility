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
			new ProjectService ( sp.GetRequiredService<IConfigService> ( ).LoadConfig ( ).ClockifyApiKey ) );
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