using System.Configuration;
using System.Data;
using System.Windows;

namespace ClockifyUtility;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
using Microsoft.Extensions.DependencyInjection;
using ClockifyUtility.Services;
using ClockifyUtility.ViewModels;

public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClockifyService, ClockifyService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<ProjectService>(sp =>
            new ProjectService(sp.GetRequiredService<IConfigService>().LoadConfig().ClockifyApiKey));
        services.AddSingleton<IInvoiceService>(sp =>
            new InvoiceService(
                sp.GetRequiredService<IClockifyService>(),
                sp.GetRequiredService<IFileService>(),
                sp.GetRequiredService<ProjectService>()
            )
        );
        services.AddSingleton<MainViewModel>();
        ServiceProvider = services.BuildServiceProvider();
        base.OnStartup(e);
    }
}

