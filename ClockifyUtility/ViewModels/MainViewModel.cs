using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.Windows;
using System.Windows.Input;
using ClockifyUtility.Services;

namespace ClockifyUtility.ViewModels
{
	// Helper class for appsettings.json
   public class AppSettings
   {
	   public string? DefaultInvoice { get; set; }
	   public string? InvoiceConfigDirectory { get; set; }
   }
	public class MainViewModel : System.ComponentModel.INotifyPropertyChanged
	{
		private readonly IConfigService _configService;
		private readonly IInvoiceService _invoiceService;
		
		private string? _selectedInvoiceConfig = null;
		private List<string> _availableInvoiceConfigs = new();
		private string? _defaultInvoiceConfig = null;
		public ICommand SetDefaultInvoiceCommand { get; }

		private string _status = string.Empty;

		public MainViewModel ( IInvoiceService invoiceService, IConfigService configService )
		{
			_invoiceService = invoiceService;
			_configService = configService;
			GenerateInvoiceCommand = new RelayCommand ( GenerateInvoiceAsync );

			// Load available configs from invoice-generator folder


		   // Load default from appsettings.json
		   var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		   if (string.IsNullOrEmpty(exeDir))
		   {
			   throw new InvalidOperationException("Could not determine executable directory.");
		   }
		   var appSettingsPath = System.IO.Path.Combine(exeDir, "appsettings.json");
		   string? invoiceConfigDir = null;
		   if (System.IO.File.Exists(appSettingsPath))
		   {
			   var json = System.IO.File.ReadAllText(appSettingsPath);
			   var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);
			   _defaultInvoiceConfig = settings?.DefaultInvoice ?? string.Empty;
			   invoiceConfigDir = settings?.InvoiceConfigDirectory;
		   }
		   if (!string.IsNullOrWhiteSpace(invoiceConfigDir) && System.IO.Directory.Exists(invoiceConfigDir))
		   {
			   var files = System.IO.Directory.GetFiles(invoiceConfigDir, "*.json");
			   _availableInvoiceConfigs = files.Select(f => System.IO.Path.GetFileName(f)).ToList();
		   }
		   else
		   {
			   _availableInvoiceConfigs = new List<string>();
		   }
		   _availableInvoiceConfigs.Insert(0, "All");
		   _selectedInvoiceConfig = _defaultInvoiceConfig ?? "All";

			SetDefaultInvoiceCommand = new RelayCommand(async () => { SetDefaultInvoice(); await Task.CompletedTask; }, () => SelectedInvoiceConfig != null && SelectedInvoiceConfig != "All");
		}
		public List<string> AvailableInvoiceConfigs
		{
			get => _availableInvoiceConfigs;
		   set { _availableInvoiceConfigs = value; OnPropertyChanged(nameof(AvailableInvoiceConfigs)); }
		}


		public string SelectedInvoiceConfig
		{
			get => _selectedInvoiceConfig ?? string.Empty;
		   set { _selectedInvoiceConfig = value; OnPropertyChanged(nameof(SelectedInvoiceConfig)); }
		}

		public string DefaultInvoiceConfig
		{
			get => _defaultInvoiceConfig ?? string.Empty;
		   set { _defaultInvoiceConfig = value; OnPropertyChanged(nameof(DefaultInvoiceConfig)); }
		}

		private bool SetDefaultInvoice()
		{
		   var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		   if (string.IsNullOrEmpty(exeDir))
		   {
			   throw new InvalidOperationException("Could not determine executable directory.");
		   }
		   var appSettingsPath = System.IO.Path.Combine(exeDir, "appsettings.json");
		   if (System.IO.File.Exists(appSettingsPath))
		   {
			   var json = System.IO.File.ReadAllText(appSettingsPath);
			   var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
			   settings.DefaultInvoice = SelectedInvoiceConfig;
			   System.IO.File.WriteAllText(appSettingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented));
			   DefaultInvoiceConfig = SelectedInvoiceConfig;
			   return true;
		   }
		   return false;
		}

		public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

		public ICommand GenerateInvoiceCommand { get; }



		public string Status
		{
			get => _status;
		   set { _status = value; OnPropertyChanged(nameof(Status)); }
		}



		private async Task GenerateInvoiceAsync ( )
		{
		   try
		   {
			   Status = "Generating invoice...";
			   Log.Information("Starting invoice generation.");
			   DateTime start = new(DateTime.Now.Year, DateTime.Now.Month, 1);
			   DateTime end = start.AddMonths(1).AddDays(-1);
			   Log.Information("Invoice period: {Start} to {End}", start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));

			   // Determine which configs to use
			   List<string> configsToGenerate = new();
			   if (SelectedInvoiceConfig == "All")
			   {
				   configsToGenerate = AvailableInvoiceConfigs.Where(f => f != "All").ToList();
			   }
			   else
			   {
				   configsToGenerate.Add(SelectedInvoiceConfig);
			   }

			   var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			   if (string.IsNullOrEmpty(exeDir))
			   {
				   throw new InvalidOperationException("Could not determine executable directory.");
			   }
			   var appSettingsPath = System.IO.Path.Combine(exeDir, "appsettings.json");
			   string? invoiceConfigDir = null;
			   if (System.IO.File.Exists(appSettingsPath))
			   {
				   var appSettingsJson = System.IO.File.ReadAllText(appSettingsPath);
				   var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(appSettingsJson);
				   invoiceConfigDir = settings?.InvoiceConfigDirectory;
			   }
			   if (!string.IsNullOrWhiteSpace(invoiceConfigDir) && System.IO.Directory.Exists(invoiceConfigDir))
			   {
				   foreach (var configFile in configsToGenerate)
				   {
					   var fullPath = System.IO.Path.Combine(invoiceConfigDir, configFile);
					   if (!System.IO.File.Exists(fullPath)) continue;
					   var json = System.IO.File.ReadAllText(fullPath);
					   var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.InvoiceConfig>(json);
					   if (config == null)
					   {
						   Log.Warning("Config file {ConfigFile} could not be deserialized and will be skipped.", configFile);
						   continue;
					   }
					   // TODO: Validate config if needed
					   string filePath = await _invoiceService.GenerateInvoiceAsync(start, end, config);
					   Status = $"Invoice generated: {filePath}";
					   Log.Information("Invoice generated at: {FilePath}", filePath);
				   }
			   }
		   }
		   catch (Services.MissingClockifyIdException ex)
		   {
			   Status = "Missing Clockify UserId or WorkspaceId.";
			   Log.Warning("Missing Clockify UserId or WorkspaceId. Querying Clockify API...");
			   await ShowClockifyIdDialogAsync(ex.ApiKey);
		   }
		   catch (Exception ex)
		   {
			   Status = $"Error: {ex.Message}";
			   Log.Error(ex, "Error generating invoice");
			   Application.Current.Dispatcher.Invoke(() =>
			   {
				   _ = MessageBox.Show($"Error generating invoice:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			   });
		   }
		}

		private async Task ShowClockifyIdDialogAsync ( string apiKey )
		{
			try
			{
				ClockifyApiService apiService = new(apiKey);
				string userId = await apiService.GetUserIdAsync();
				List<Models.WorkspaceInfo> workspaces = await apiService.GetWorkspacesAsync ( );
				System.Windows.Application.Current.Dispatcher.Invoke ( ( ) =>
				{
					Views.ClockifyIdDialog dialog = new ( userId, workspaces );
					_ = dialog.ShowDialog ( );
				} );
				Serilog.Log.Information("Displayed Clockify ID dialog.");
			}
			catch ( Exception ex )
			{
				Serilog.Log.Error(ex, "Error fetching Clockify IDs");
				System.Windows.Application.Current.Dispatcher.Invoke ( ( ) =>
				{
					_ = System.Windows.MessageBox.Show ( $"Could not connect to Clockify. Please check your API key and internet connection.\n\nError details:\n{ex.Message}", "Clockify Connection Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
				} );
			}
		}

		protected void OnPropertyChanged ( string propertyName )
		{
			PropertyChanged?.Invoke ( this, new System.ComponentModel.PropertyChangedEventArgs ( propertyName ) );
		}
	}

	// Simple RelayCommand implementation
	public class RelayCommand : ICommand
	{
		private readonly Func<bool>? _canExecute;
		private readonly Func<Task> _execute;

		public RelayCommand ( Func<Task> execute, Func<bool>? canExecute = null )
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public event EventHandler? CanExecuteChanged;

		public bool CanExecute ( object? parameter )
		{
			return _canExecute == null || _canExecute ( );
		}

		public async void Execute ( object? parameter )
		{
			await _execute ( );
		}
	}
}