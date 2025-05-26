using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.Windows;
using System.Windows.Input;
using ClockifyUtility.Services;
using ClockifyUtility.Models;
using ClockifyUtility.Helpers;

namespace ClockifyUtility.ViewModels
{
	// Helper class for appsettings.json
	public class AppSettings
	{
		public string? DefaultInvoice { get; set; }
		public string? InvoiceConfigDirectory { get; set; }
		public string? InvoiceNumber { get; set; } // Added for invoice number tracking
	}

	// MainViewModel and RelayCommand restored below
	public class MainViewModel : System.ComponentModel.INotifyPropertyChanged
	{
		// --- Star Icon Properties for Default Invoice ---
		public string StarIconUnicode => IsDefaultInvoiceConfigSelected ? "\uF005" : "\uF006"; // F005 = solid, F006 = regular
		public System.Windows.Media.FontFamily StarIconFontFamily =>
			( System.Windows.Media.FontFamily ) System.Windows.Application.Current.Resources [ IsDefaultInvoiceConfigSelected ? "FontAwesomeSolid" : "FontAwesomeRegular" ];

		public bool IsDefaultInvoiceConfigSelected =>
			!string.IsNullOrEmpty ( _selectedInvoiceConfig ) &&
			_selectedInvoiceConfig == ( _defaultInvoiceConfig ?? string.Empty );

		// --- Fields ---
		private DateTime _selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
		private string? _selectedInvoiceConfig = null;
		private List<string> _availableInvoiceConfigs = new();
		private string? _defaultInvoiceConfig = null;
		private string _status = string.Empty;
		private string _generateButtonText = "Generate Invoice";
		private bool _isGenerateButtonEnabled = true;
		private List<string> _displayInvoiceConfigs = new();
		private double _generateButtonFontSize = 14.0;

		private readonly IConfigService _configService;
		private readonly IInvoiceService _invoiceService;

		// --- Properties ---
		public DateTime SelectedMonth
		{
			get => _selectedMonth;
			set
			{
				if ( _selectedMonth != value )
				{
					_selectedMonth = value;
					OnPropertyChanged ( nameof ( SelectedMonth ) );
					OnPropertyChanged ( nameof ( CurrentMonthLabel ) );
				}
			}
		}

		public string CurrentMonthLabel => SelectedMonth.ToString ( "MMMM yyyy" );

		public ICommand PreviousMonthCommand { get; }
		public ICommand NextMonthCommand { get; }
		public ICommand SetDefaultInvoiceCommand { get; }
		public ICommand GenerateInvoiceCommand { get; }

		public string GenerateButtonText
		{
			get => _generateButtonText;
			set { _generateButtonText = value; OnPropertyChanged ( nameof ( GenerateButtonText ) ); }
		}

		public bool IsGenerateButtonEnabled
		{
			get => _isGenerateButtonEnabled;
			set { _isGenerateButtonEnabled = value; OnPropertyChanged ( nameof ( IsGenerateButtonEnabled ) ); ( ( RelayCommand ) GenerateInvoiceCommand ).RaiseCanExecuteChanged ( ); }
		}

		public string Status
		{
			get => _status;
			set { _status = value; OnPropertyChanged ( nameof ( Status ) ); }
		}

		public List<string> DisplayInvoiceConfigs
		{
			get => _displayInvoiceConfigs;
			set { _displayInvoiceConfigs = value; OnPropertyChanged ( nameof ( DisplayInvoiceConfigs ) ); }
		}

		public string SelectedInvoiceConfig
		{
			get
			{
				var current = _selectedInvoiceConfig ?? string.Empty;
				var match = DisplayInvoiceConfigs.FirstOrDefault(d => d.Replace(" (Default)", "") == current);
				return match ?? current;
			}
			set
			{
				var cleanValue = value?.Replace(" (Default)", "");
				_selectedInvoiceConfig = cleanValue;
				OnPropertyChanged ( nameof ( SelectedInvoiceConfig ) );
				OnPropertyChanged ( nameof ( IsDefaultInvoiceConfigSelected ) );
				OnPropertyChanged ( nameof ( StarIconUnicode ) );
				OnPropertyChanged ( nameof ( StarIconFontFamily ) );
			}
		}

		public string DefaultInvoiceConfig
		{
			get => _defaultInvoiceConfig ?? string.Empty;
			set
			{
				_defaultInvoiceConfig = value;
				OnPropertyChanged ( nameof ( DefaultInvoiceConfig ) );
				OnPropertyChanged ( nameof ( IsDefaultInvoiceConfigSelected ) );
				OnPropertyChanged ( nameof ( StarIconUnicode ) );
				OnPropertyChanged ( nameof ( StarIconFontFamily ) );
			}
		}

		public double GenerateButtonFontSize
		{
			get => _generateButtonFontSize;
			set { _generateButtonFontSize = value; OnPropertyChanged(nameof(GenerateButtonFontSize)); }
		}

		// --- Constructor ---
		public MainViewModel ( IInvoiceService invoiceService, IConfigService configService )
		{
			_invoiceService = invoiceService;
			_configService = configService;
			GenerateInvoiceCommand = new RelayCommand ( GenerateInvoiceAsync, ( ) => IsGenerateButtonEnabled );
			GenerateButtonText = "Generate Invoice";
			IsGenerateButtonEnabled = true;
			PreviousMonthCommand = new RelayCommand ( async ( ) => { SelectedMonth = SelectedMonth.AddMonths ( -1 ); await Task.CompletedTask; }, ( ) => true );
			NextMonthCommand = new RelayCommand ( async ( ) => { SelectedMonth = SelectedMonth.AddMonths ( 1 ); await Task.CompletedTask; }, ( ) => true );
			// Load configs
			var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			if ( string.IsNullOrEmpty ( exeDir ) )
				throw new InvalidOperationException ( "Could not determine executable directory." );
			var appSettingsPath = System.IO.Path.Combine(exeDir, "appsettings.json");
			string? invoiceConfigDir = null;
			if ( System.IO.File.Exists ( appSettingsPath ) )
			{
				var json = System.IO.File.ReadAllText(appSettingsPath);
				var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);
				_defaultInvoiceConfig = settings?.DefaultInvoice ?? string.Empty;
				invoiceConfigDir = settings?.InvoiceConfigDirectory;
			}
			if ( !string.IsNullOrWhiteSpace ( invoiceConfigDir ) && System.IO.Directory.Exists ( invoiceConfigDir ) )
			{
				var files = System.IO.Directory.GetFiles(invoiceConfigDir, "*.json")
					.Where(f => !System.IO.Path.GetFileName(f).StartsWith("appsettings.", StringComparison.OrdinalIgnoreCase))
					.ToList();
				_availableInvoiceConfigs = files.Select ( f => System.IO.Path.GetFileName ( f ) ).ToList ( );
			}
			else
			{
				_availableInvoiceConfigs = new List<string> ( );
			}
			_availableInvoiceConfigs.Insert ( 0, "All" );
			UpdateDisplayInvoiceConfigs ( );
			if ( !string.IsNullOrEmpty ( _defaultInvoiceConfig ) && _availableInvoiceConfigs.Contains ( _defaultInvoiceConfig ) )
				_selectedInvoiceConfig = _defaultInvoiceConfig;
			else if ( _defaultInvoiceConfig == "All" )
				_selectedInvoiceConfig = "All";
			else
				_selectedInvoiceConfig = "All";
			SetDefaultInvoiceCommand = new RelayCommand ( async ( ) => { SetDefaultInvoice ( ); await Task.CompletedTask; }, ( ) => true );
		}

		// --- Methods ---
		private void UpdateDisplayInvoiceConfigs ( )
		{
			DisplayInvoiceConfigs = _availableInvoiceConfigs
				.Select ( cfg => cfg == ( _defaultInvoiceConfig ?? "" ) ? $"{cfg} (Default)" : cfg )
				.ToList ( );
			var current = _selectedInvoiceConfig ?? string.Empty;
			var match = DisplayInvoiceConfigs.FirstOrDefault(d => d.Replace(" (Default)", "") == current);
			if ( !string.IsNullOrEmpty ( match ) )
			{
				_selectedInvoiceConfig = match.Replace ( " (Default)", "" );
				OnPropertyChanged ( nameof ( SelectedInvoiceConfig ) );
				OnPropertyChanged ( nameof ( IsDefaultInvoiceConfigSelected ) );
				OnPropertyChanged ( nameof ( StarIconUnicode ) );
				OnPropertyChanged ( nameof ( StarIconFontFamily ) );
			}
		}

		private bool SetDefaultInvoice ( )
		{
			var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			if ( string.IsNullOrEmpty ( exeDir ) )
				throw new InvalidOperationException ( "Could not determine executable directory." );
			var appSettingsPath = System.IO.Path.Combine(exeDir, "appsettings.json");
			if ( System.IO.File.Exists ( appSettingsPath ) )
			{
				var json = System.IO.File.ReadAllText(appSettingsPath);
				var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
				var cleanValue = SelectedInvoiceConfig?.Replace(" (Default)", "");
				settings.DefaultInvoice = cleanValue;
				System.IO.File.WriteAllText ( appSettingsPath, Newtonsoft.Json.JsonConvert.SerializeObject ( settings, Newtonsoft.Json.Formatting.Indented ) );
				DefaultInvoiceConfig = cleanValue ?? string.Empty;
				UpdateDisplayInvoiceConfigs ( );
				var match = DisplayInvoiceConfigs.FirstOrDefault(d => d.Replace(" (Default)", "") == cleanValue);
				if ( !string.IsNullOrEmpty ( match ) )
				{
					_selectedInvoiceConfig = cleanValue;
					OnPropertyChanged ( nameof ( SelectedInvoiceConfig ) );
					OnPropertyChanged ( nameof ( IsDefaultInvoiceConfigSelected ) );
					OnPropertyChanged ( nameof ( StarIconUnicode ) );
					OnPropertyChanged ( nameof ( StarIconFontFamily ) );
				}
				Status = $"Default invoice updated to: {cleanValue}";
				return true;
			}
			Status = "Failed to update default invoice (appsettings.json not found).";
			return false;
		}

		private async Task GenerateInvoiceAsync ( )
		{
			bool clockifyIdPopupShown = false;
			try
			{
				IsGenerateButtonEnabled = false;
				GenerateButtonFontSize = 12.0;
				GenerateButtonText = "Generating...";
				Status = "Generating invoice...";
				Serilog.Log.Information ( "Starting invoice generation." );
				DateTime start = new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
				DateTime end = new DateTime(SelectedMonth.Year, SelectedMonth.Month, DateTime.DaysInMonth(SelectedMonth.Year, SelectedMonth.Month), 23, 59, 59, DateTimeKind.Utc);
				Serilog.Log.Information ( "Invoice period (UTC): {Start} to {End}", start.ToString ( "yyyy-MM-ddTHH:mm:ssZ" ), end.ToString ( "yyyy-MM-ddTHH:mm:ssZ" ) );
				string selectedConfigName = _selectedInvoiceConfig ?? "All";
				List<string> configsToGenerate = new();
				if ( selectedConfigName == "All" )
				{
					configsToGenerate = _availableInvoiceConfigs.Where ( f => f != "All" ).ToList ( );
				}
				else
				{
					configsToGenerate.Add ( selectedConfigName );
				}
				var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				if ( string.IsNullOrEmpty ( exeDir ) )
					throw new InvalidOperationException ( "Could not determine executable directory." );
				var appSettingsPath = System.IO.Path.Combine(exeDir, "appsettings.json");
				string? invoiceConfigDir = null;
				if ( System.IO.File.Exists ( appSettingsPath ) )
				{
					var appSettingsJson = System.IO.File.ReadAllText(appSettingsPath);
					var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(appSettingsJson);
					invoiceConfigDir = settings?.InvoiceConfigDirectory;
				}
				if ( !string.IsNullOrWhiteSpace ( invoiceConfigDir ) && System.IO.Directory.Exists ( invoiceConfigDir ) )
				{
					var skippedConfigs = new List<string>();
					foreach ( var configFile in configsToGenerate )
					{
						var fullPath = System.IO.Path.Combine(invoiceConfigDir, configFile);
						if ( !System.IO.File.Exists ( fullPath ) ) continue;
						var json = System.IO.File.ReadAllText(fullPath);
						var config = Newtonsoft.Json.JsonConvert.DeserializeObject<InvoiceConfig>(json);
						if ( config == null )
						{
							Serilog.Log.Warning ( "Config file {ConfigFile} could not be deserialized and will be skipped.", configFile );
							skippedConfigs.Add ( configFile + " (invalid JSON)" );
							continue;
						}
						var errors = InvoiceConfigValidator.Validate(config);
						if ( errors.Count > 0 )
						{
							Serilog.Log.Warning ( "Config file {ConfigFile} is invalid and will be skipped: {Errors}", configFile, string.Join ( "; ", errors ) );
							skippedConfigs.Add ( configFile + ": " + string.Join ( ", ", errors ) );
							// If missing UserId or WorkspaceId, show popup (once)
							if (!clockifyIdPopupShown && (errors.Any(e => e.Contains("UserId is required.")) || errors.Any(e => e.Contains("WorkspaceId is required."))))
							{
								clockifyIdPopupShown = true;
								// Try to get API key from config if possible
								string apiKey = config.Clockify?.ClockifyApiKey ?? string.Empty;
								await ShowClockifyIdMessageAsync(apiKey);
							}
							continue;
						}
						// Use centralized appsettings.json for invoice number
						var appSettingsJson = System.IO.File.ReadAllText(appSettingsPath);
						var appSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(appSettingsJson) ?? new AppSettings();
						string currentInvoiceNumber = appSettings.InvoiceNumber ?? "000";
						if (!int.TryParse(currentInvoiceNumber, out int num))
							num = 0;
						num++;
						string nextInvoiceNumber = num.ToString("D3");
						appSettings.InvoiceNumber = nextInvoiceNumber;
						System.IO.File.WriteAllText(appSettingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(appSettings, Newtonsoft.Json.Formatting.Indented));
						string clientName = config.Clockify?.ClientName ?? "Unknown Client";
						Status = $"Generating invoice for {clientName}...";
						GenerateButtonText = $"Processing {clientName}...";
						// Set InvoiceNumber on config.Clockify from appsettings value
						config.Clockify.InvoiceNumber = nextInvoiceNumber;
						var (html, pdfFilePath) = await _invoiceService.GenerateInvoiceAsync ( start, end, config );
						// Optionally, you could use the html string here if needed (e.g., preview)
					}
					if ( skippedConfigs.Count > 0 )
					{
						Status = $"Invoice(s) saved successfully. Skipped: {skippedConfigs.Count} config(s):\n" + string.Join ( "\n", skippedConfigs );
						GenerateButtonText = "Completed (with Skips)";
					}
					else
					{
						Status = "Invoice(s) saved successfully";
						GenerateButtonText = "Completed";
					}
				}
			}
			catch ( Services.MissingClockifyIdException ex )
			{
				Status = "Missing Clockify UserId or WorkspaceId.";
				GenerateButtonText = "Error";
				Serilog.Log.Warning ( "Missing Clockify UserId or WorkspaceId. Querying Clockify API..." );
				await ShowClockifyIdMessageAsync ( ex.ApiKey );
			}
			catch ( Exception ex )
			{
				Status = $"Error: {ex.Message}";
				GenerateButtonText = "Error";
				Serilog.Log.Error ( ex, "Error generating invoice" );
				Application.Current.Dispatcher.Invoke ( ( ) =>
				{
					_ = MessageBox.Show ( $"Error generating invoice:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
				} );
			}
			finally
			{
				await Task.Delay ( 1200 );
				GenerateButtonText = "Generate Invoice";
				GenerateButtonFontSize = 14.0;
				IsGenerateButtonEnabled = true;
			}
		}

		private async Task ShowClockifyIdMessageAsync ( string apiKey )
		{
			try
			{
				ClockifyApiService apiService = new(apiKey);
				string userId = await apiService.GetUserIdAsync();
				List<Models.WorkspaceInfo> workspaces = await apiService.GetWorkspacesAsync();
				string message =
					"Your invoice configuration is missing the required Clockify User ID or Workspace ID.\n\n" +
					"Below are the User ID and available workspaces for your account.\n" +
					"Please copy these values and add them to your invoice config file.\n" +
					"Once added, the invoice will be generated. Until then, the invoice will be skipped.\n\n" +
					$"Clockify User ID: {userId}\n";
				if (workspaces.Count == 0)
				{
					message += "No workspaces found.";
				}
				else
				{
					foreach (var ws in workspaces)
					{
						message += $"- {ws.Name} (ID: {ws.Id})\n";
					}
				}
				System.Windows.Application.Current.Dispatcher.Invoke(() =>
				{
					System.Windows.MessageBox.Show(message, "Clockify User and Workspace Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
				});
				Serilog.Log.Information("Displayed Clockify User and Workspace info message box.");
			}
			catch ( Exception ex )
			{
				Serilog.Log.Error ( ex, "Error fetching Clockify IDs" );
				System.Windows.Application.Current.Dispatcher.Invoke ( ( ) =>
				{
					_ = System.Windows.MessageBox.Show ( $"Could not connect to Clockify. Please check your API key and internet connection.\n\nError details:\n{ex.Message}", "Clockify Connection Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
				} );
			}
		}

		// Commented out the old ShowClockifyIdDialogAsync method
		/*
		private async Task ShowClockifyIdDialogAsync ( string apiKey )
		{
			try
			{
				ClockifyApiService apiService = new(apiKey);
				string userId = await apiService.GetUserIdAsync();
				List<Models.WorkspaceInfo> workspaces = await apiService.GetWorkspacesAsync();
				System.Windows.Application.Current.Dispatcher.Invoke ( ( ) =>
				{
					Views.ClockifyIdDialog dialog = new(userId, workspaces);
					_ = dialog.ShowDialog ( );
				} );
				Serilog.Log.Information ( "Displayed Clockify ID dialog." );
			}
			catch ( Exception ex )
			{
				Serilog.Log.Error ( ex, "Error fetching Clockify IDs" );
				System.Windows.Application.Current.Dispatcher.Invoke ( ( ) =>
				{
					_ = System.Windows.MessageBox.Show ( $"Could not connect to Clockify. Please check your API key and internet connection.\n\nError details:\n{ex.Message}", "Clockify Connection Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
				} );
			}
		}
		*/

		public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
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

		public void RaiseCanExecuteChanged ( )
		{
			CanExecuteChanged?.Invoke ( this, EventArgs.Empty );
		}
	}
}