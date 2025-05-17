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
        services.AddSingleton<IInvoiceService, InvoiceService>();
        services.AddSingleton<MainViewModel>();
        ServiceProvider = services.BuildServiceProvider();
        base.OnStartup(e);
    }
}

