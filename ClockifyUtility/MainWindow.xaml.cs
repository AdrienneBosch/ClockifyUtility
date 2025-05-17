using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClockifyUtility;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
using ClockifyUtility.Services;
using ClockifyUtility.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Use DI to resolve MainViewModel
        var viewModel = App.ServiceProvider.GetService(typeof(MainViewModel)) as MainViewModel;
        this.DataContext = viewModel;
    }
}