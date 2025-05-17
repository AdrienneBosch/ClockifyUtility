using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ClockifyUtility.Models;
using ClockifyUtility.Services;

namespace ClockifyUtility.ViewModels
{
    public class MainViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IConfigService _configService;
        public ICommand GenerateInvoiceCommand { get; }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private string _log = string.Empty;
        public string Log
        {
            get => _log;
            set { _log = value; OnPropertyChanged(nameof(Log)); }
        }

        public MainViewModel(IInvoiceService invoiceService, IConfigService configService)
        {
            _invoiceService = invoiceService;
            _configService = configService;
            GenerateInvoiceCommand = new RelayCommand(async () => await GenerateInvoiceAsync());
        }

        private void AppendLog(string message)
        {
            Log += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        private async Task GenerateInvoiceAsync()
        {
            try
            {
                Status = "Generating invoice...";
                AppendLog("Starting invoice generation.");
                var config = _configService.LoadConfig();
                AppendLog("Loaded configuration.");
                var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);
                AppendLog($"Invoice period: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
                var filePath = await _invoiceService.GenerateInvoiceAsync(start, end, config);
                Status = $"Invoice generated: {filePath}";
                AppendLog($"Invoice generated at: {filePath}");
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
                AppendLog($"Error: {ex.Message}");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Error generating invoice:\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        public event EventHandler? CanExecuteChanged;
        public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public async void Execute(object? parameter) => await _execute();
    }
}
