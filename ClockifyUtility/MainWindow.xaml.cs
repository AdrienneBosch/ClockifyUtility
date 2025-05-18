using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
using ClockifyUtility.ViewModels;

namespace ClockifyUtility
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			// Use DI to resolve MainViewModel
			MainViewModel? viewModel = App.ServiceProvider?.GetService(typeof(MainViewModel)) as MainViewModel;
			if (viewModel == null)
			{
				throw new InvalidOperationException("MainViewModel could not be resolved from the service provider.");
			}
			DataContext = viewModel;
		}
	}
}