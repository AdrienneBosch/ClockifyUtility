using System;
using System.Windows;
using System.Windows.Controls;

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
                        UpdateThemeSelection(ThemeManager.RequestedTheme, ThemeManager.EffectiveTheme);
                }

                private bool _isSynchronizingTheme;

                private void OnThemeManagerThemeChanged(object? sender, ThemeChangedEventArgs e)
                {
                        Dispatcher.Invoke(() => UpdateThemeSelection(e.RequestedTheme, e.EffectiveTheme));
                }

                private void ThemeOptionRadioButton_Checked(object sender, RoutedEventArgs e)
                {
                        if (_isSynchronizingTheme)
                        {
                                return;
                        }

                        if (sender is RadioButton radioButton && radioButton.IsChecked == true && radioButton.Tag is string tag && Enum.TryParse(tag, out AppTheme theme))
                        {
                                ThemeManager.ApplyTheme(Application.Current, theme);
                        }
                }

                private void UpdateThemeSelection(AppTheme requestedTheme, AppTheme effectiveTheme)
                {
                        if (SystemThemeRadio is null || LightThemeRadio is null || DarkThemeRadio is null)
                        {
                                return;
                        }

                        _isSynchronizingTheme = true;

                        try
                        {
                                SystemThemeRadio.IsChecked = requestedTheme == AppTheme.System;
                                LightThemeRadio.IsChecked = requestedTheme == AppTheme.Light;
                                DarkThemeRadio.IsChecked = requestedTheme == AppTheme.Dark;

                                if (SystemThemeDetailText is not null)
                                {
                                        string detail = requestedTheme == AppTheme.System
                                                ? effectiveTheme switch
                                                {
                                                        AppTheme.Dark => "OS · Dark",
                                                        AppTheme.Light => "OS · Light",
                                                        _ => "Matches OS"
                                                }
                                                : "Matches OS";

                                        SystemThemeDetailText.Text = detail;
                                }
                        }
                        finally
                        {
                                _isSynchronizingTheme = false;
                        }
                }

                protected override void OnClosed(EventArgs e)
                {
                        ThemeManager.ThemeChanged -= OnThemeManagerThemeChanged;
                        base.OnClosed(e);
                }
        }
}
