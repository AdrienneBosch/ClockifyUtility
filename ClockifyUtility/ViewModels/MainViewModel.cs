using System.Windows.Input;

using ClockifyUtility.Services;

namespace ClockifyUtility.ViewModels
{
	public class MainViewModel : System.ComponentModel.INotifyPropertyChanged
	{
		private readonly IConfigService _configService;
		private readonly IInvoiceService _invoiceService;
		private string _log = string.Empty;
		private string _status = string.Empty;

		public MainViewModel ( IInvoiceService invoiceService, IConfigService configService )
		{
			_invoiceService = invoiceService;
			_configService = configService;
			GenerateInvoiceCommand = new RelayCommand ( GenerateInvoiceAsync );
		}

		public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

		public ICommand GenerateInvoiceCommand { get; }

		public string Log
		{
			get => _log;
			set { _log = value; OnPropertyChanged ( nameof ( Log ) ); }
		}

		public string Status
		{
			get => _status;
			set { _status = value; OnPropertyChanged ( nameof ( Status ) ); }
		}

		private void AppendLog ( string message )
		{
			Log += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
			System.Diagnostics.Debug.WriteLine ( $"[LOG] {message}" );
		}

		private async Task GenerateInvoiceAsync ( )
		{
			try
			{
				Status = "Generating invoice...";
				AppendLog ( "Starting invoice generation." );
				Models.ConfigModel config = _configService.LoadConfig ( );
				AppendLog ( "Loaded configuration." );
				DateTime start = new(DateTime.Now.Year, DateTime.Now.Month, 1);
				DateTime end = start.AddMonths(1).AddDays(-1);
				AppendLog ( $"Invoice period: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}" );

				// Pass AppendLog to InvoiceService for deep logging
				string filePath = await _invoiceService.GenerateInvoiceAsync(start, end, config, AppendLog);
				Status = $"Invoice generated: {filePath}";
				AppendLog ( $"Invoice generated at: {filePath}" );
			}
			catch ( Services.MissingClockifyIdException ex )
			{
				Status = "Missing Clockify UserId or WorkspaceId.";
				AppendLog ( "Missing Clockify UserId or WorkspaceId. Querying Clockify API..." );
				await ShowClockifyIdDialogAsync ( ex.ApiKey );
			}
			catch ( Exception ex )
			{
				Status = $"Error: {ex.Message}";
				AppendLog ( $"Error: {ex.Message}" );
				System.Windows.Application.Current.Dispatcher.Invoke ( ( ) =>
				{
					_ = System.Windows.MessageBox.Show ( $"Error generating invoice:\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
				} );
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
				AppendLog ( "Displayed Clockify ID dialog." );
			}
			catch ( Exception ex )
			{
				AppendLog ( $"Error fetching Clockify IDs: {ex.Message}" );
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