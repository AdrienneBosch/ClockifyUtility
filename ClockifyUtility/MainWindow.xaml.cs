using System.Windows;

using ClockifyUtility.Helpers;
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

                        ThemeManager.ThemeChanged += OnThemeManagerThemeChanged;
                        UpdateThemeButton(ThemeManager.RequestedTheme, ThemeManager.EffectiveTheme);
                }

                private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
                {
                        ThemeManager.CycleTheme(Application.Current);
                }

                private void OnThemeManagerThemeChanged(object? sender, ThemeChangedEventArgs e)
                {
                        Dispatcher.Invoke(() => UpdateThemeButton(e.RequestedTheme, e.EffectiveTheme));
                }

                private void UpdateThemeButton(AppTheme requestedTheme, AppTheme effectiveTheme)
                {
                        if (ThemeIcon is null || ThemeLabel is null)
                        {
                                return;
                        }

                        string iconGlyph = requestedTheme switch
                        {
                                AppTheme.Light => "\uf185",
                                AppTheme.Dark => "\uf186",
                                _ => "\uf109"
                        };

                        string label = requestedTheme switch
                        {
                                AppTheme.Light => "Light",
                                AppTheme.Dark => "Dark",
                                _ => effectiveTheme switch
                                {
                                        AppTheme.Dark => "System · Dark",
                                        AppTheme.Light => "System · Light",
                                        _ => "System"
                                }
                        };

                        ThemeIcon.Text = iconGlyph;
                        ThemeLabel.Text = label;
                }

                protected override void OnClosed(EventArgs e)
                {
                        ThemeManager.ThemeChanged -= OnThemeManagerThemeChanged;
                        base.OnClosed(e);
                }
        }
}
