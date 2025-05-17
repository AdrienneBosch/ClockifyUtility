using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
using ClockifyUtility.ViewModels;

namespace ClockifyUtility;
public partial class MainWindow : Window
{
	public MainWindow ( )
	{
		InitializeComponent ( );

		// Use DI to resolve MainViewModel
		MainViewModel? viewModel = App.ServiceProvider.GetService ( typeof ( MainViewModel ) ) as MainViewModel;
		DataContext = viewModel;
	}
}