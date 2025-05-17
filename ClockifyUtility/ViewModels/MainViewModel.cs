using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ClockifyUtility.Models;
using ClockifyUtility.Services;

namespace ClockifyUtility.ViewModels
{
    public class MainViewModel
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IConfigService _configService;
        public ICommand GenerateInvoiceCommand { get; }
        public string Status { get; set; }

        public MainViewModel(IInvoiceService invoiceService, IConfigService configService)
        {
            _invoiceService = invoiceService;
            _configService = configService;
            GenerateInvoiceCommand = new RelayCommand(async () => await GenerateInvoiceAsync());
        }

        private async Task GenerateInvoiceAsync()
        {
            try
            {
                Status = "Generating invoice...";
                var config = _configService.LoadConfig();
                var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);
                var filePath = await _invoiceService.GenerateInvoiceAsync(start, end, config);
                Status = $"Invoice generated: {filePath}";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Error generating invoice:\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
        }
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
